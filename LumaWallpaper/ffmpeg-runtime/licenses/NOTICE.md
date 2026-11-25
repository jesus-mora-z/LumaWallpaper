# Third-party media runtime notices

## FFmpeg

Luma uses FFmpeg 8.0.1.3 from `DevEnvy.FFmpeg.Binaries.LGPL` to convert
incompatible videos to H.264. The package's third-party notices and LGPL
license information are copied to `ffmpeg\licenses` beside the application.
Source and build scripts: https://github.com/devenvy/ffmpeg-dotnet
FFmpeg source: https://github.com/FFmpeg/FFmpeg

## MinGW runtime libraries

The following runtime DLLs are distributed alongside FFmpeg and remain
replaceable:

- `libstdc++-6.dll` and `libgcc_s_seh-1.dll`, MSYS2
  `mingw-w64-x86_64-libstdc++` and `mingw-w64-x86_64-libgcc` 16.2.0-7.
- `libwinpthread-1.dll`, MSYS2 `mingw-w64-x86_64-libwinpthread`
  14.0.0.r598.ge3d18511f-1.

Their license texts are in the matching folders in `ffmpeg\licenses`.
Package sources and build scripts: https://github.com/msys2/MINGW-packages
