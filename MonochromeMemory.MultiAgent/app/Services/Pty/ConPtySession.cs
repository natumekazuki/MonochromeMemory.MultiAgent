using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace CodexMultiAgent.App.Services.Pty;

public sealed class ConPtySession : IAsyncDisposable
{
    private ConPtyNative.SafePseudoConsoleHandle? _pseudoConsole;
    private SafeFileHandle? _ptyInputWrite;
    private SafeFileHandle? _ptyOutputRead;
    private FileStream? _inputStream;
    private FileStream? _outputStream;
    private Task? _readTask;
    private CancellationTokenSource? _readCts;
    private bool _started;
    private ConPtyNative.ProcessInformation _processInfo;

    public event Action<string>? OutputReceived;

    public uint ProcessId => _processInfo.dwProcessId;

    public async Task StartAsync(PtyStartOptions options, CancellationToken cancellationToken = default)
    {
        if (_started)
        {
            throw new InvalidOperationException("Session already started.");
        }

        if (string.IsNullOrWhiteSpace(options.Executable))
        {
            throw new ArgumentException("Executable is required.", nameof(options));
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("ConPTY is supported only on Windows.");
        }

        var ptyInputRead = default(SafeFileHandle);
        var ptyOutputWrite = default(SafeFileHandle);

        if (!ConPtyNative.CreatePipe(out ptyInputRead, out _ptyInputWrite!, IntPtr.Zero, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!ConPtyNative.CreatePipe(out _ptyOutputRead!, out ptyOutputWrite, IntPtr.Zero, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!ConPtyNative.SetHandleInformation(_ptyInputWrite!, ConPtyNative.HandleFlagInherit, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!ConPtyNative.SetHandleInformation(_ptyOutputRead!, ConPtyNative.HandleFlagInherit, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var coord = new ConPtyNative.Coord
        {
            X = (short)options.Columns,
            Y = (short)options.Rows
        };

        var hr = ConPtyNative.CreatePseudoConsole(coord, ptyInputRead, ptyOutputWrite, 0, out _pseudoConsole!);
        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        ptyInputRead.Dispose();
        ptyOutputWrite.Dispose();

        var attributeListSize = IntPtr.Zero;
        ConPtyNative.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeListSize);
        var attributeList = Marshal.AllocHGlobal(attributeListSize);
        try
        {
            if (!ConPtyNative.InitializeProcThreadAttributeList(attributeList, 1, 0, ref attributeListSize))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var attribute = new IntPtr(ConPtyNative.ProcThreadAttributePseudoConsole);
            var pseudoConsoleHandle = _pseudoConsole.DangerousGetHandle();
            var pseudoConsolePtr = Marshal.AllocHGlobal(IntPtr.Size);
            try
            {
                Marshal.WriteIntPtr(pseudoConsolePtr, pseudoConsoleHandle);
                if (!ConPtyNative.UpdateProcThreadAttributeList(
                    attributeList,
                    0,
                    attribute,
                    pseudoConsolePtr,
                    (IntPtr)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pseudoConsolePtr);
            }

            var startupInfo = new ConPtyNative.StartupInfoEx
            {
                StartupInfo = new ConPtyNative.StartupInfo
                {
                    cb = (uint)Marshal.SizeOf<ConPtyNative.StartupInfoEx>()
                },
                lpAttributeList = attributeList
            };

            var commandLine = CommandLineBuilder.Build(options.Executable, options.Arguments);

            if (!ConPtyNative.CreateProcessW(
                null,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                ConPtyNative.ExtendedStartupInfoPresent | ConPtyNative.CreateUnicodeEnvironment,
                IntPtr.Zero,
                options.WorkingDirectory,
                ref startupInfo,
                out _processInfo))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            if (attributeList != IntPtr.Zero)
            {
                ConPtyNative.DeleteProcThreadAttributeList(attributeList);
                Marshal.FreeHGlobal(attributeList);
            }
        }

        _inputStream = new FileStream(_ptyInputWrite!, FileAccess.Write, 4096, true);
        _outputStream = new FileStream(_ptyOutputRead!, FileAccess.Read, 4096, true);
        _readCts = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadLoopAsync(_readCts.Token), _readCts.Token);
        _started = true;

        await Task.Yield();
    }

    public async Task WriteAsync(string input, CancellationToken cancellationToken = default)
    {
        if (!_started || _inputStream == null)
        {
            throw new InvalidOperationException("Session not started.");
        }

        var bytes = Encoding.UTF8.GetBytes(input);
        await _inputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        await _inputStream.FlushAsync(cancellationToken);
    }

    public void Resize(short columns, short rows)
    {
        if (_pseudoConsole == null)
        {
            return;
        }

        var coord = new ConPtyNative.Coord { X = columns, Y = rows };
        var hr = ConPtyNative.ResizePseudoConsole(_pseudoConsole, coord);
        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    public async Task StopAsync()
    {
        if (!_started)
        {
            return;
        }

        _readCts?.Cancel();
        if (_readTask != null)
        {
            await _readTask.ConfigureAwait(false);
        }

        DisposeHandles();
        _started = false;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task ReadLoopAsync(CancellationToken token)
    {
        if (_outputStream == null)
        {
            return;
        }

        var buffer = new byte[4096];
        try
        {
            while (!token.IsCancellationRequested)
            {
                var bytesRead = await _outputStream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead <= 0)
                {
                    break;
                }

                var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                OutputReceived?.Invoke(text);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }
        catch (IOException)
        {
            // ignore
        }
    }

    private void DisposeHandles()
    {
        try
        {
            _inputStream?.Dispose();
            _outputStream?.Dispose();
        }
        catch
        {
            // ignore
        }

        _ptyInputWrite?.Dispose();
        _ptyOutputRead?.Dispose();
        _pseudoConsole?.Dispose();

        if (_processInfo.hThread != IntPtr.Zero)
        {
            ConPtyNative.CloseHandle(_processInfo.hThread);
            _processInfo.hThread = IntPtr.Zero;
        }

        if (_processInfo.hProcess != IntPtr.Zero)
        {
            ConPtyNative.CloseHandle(_processInfo.hProcess);
            _processInfo.hProcess = IntPtr.Zero;
        }
    }
}
