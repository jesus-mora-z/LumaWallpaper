# Luma Wallpaper

[Español](README.md) | [English](README.en.md)

Convierte tus propios videos GIF y MP4 en un fondo animado para Windows.
Luma los reproduce en bucle detrás de los iconos del escritorio, sin audio,
y puede restaurar el último fondo e iniciarse con Windows.

## Descargar

- [Descargar la última versión desde GitHub Releases](https://github.com/jesus-mora-z/LumaWallpaper/releases/latest)
- [Copia alternativa en MediaFire](https://www.mediafire.com/file/fvr62lncjaik3ma/LumaWallpaper-Setup-1.0.0-x64.exe/file)

El instalador para Windows x64 incluye .NET y no necesita permisos de
administrador. No está firmado digitalmente; Windows puede mostrar una
advertencia de SmartScreen.

## Características

- Usa tus propios archivos `.gif` y `.mp4`.
- Reproduce videos en bucle y sin audio.
- Mantiene el fondo activo desde el área de notificaciones.
- Puede recordar el último fondo e iniciarse con Windows.
- Incluye interfaz en español, inglés, portugués brasileño, francés, alemán,
  italiano y japonés; la selección se guarda para la próxima sesión.
- En equipos compatibles, usa DirectComposition y decodificación de video por
  hardware.
- Si un video no se puede reproducir, crea una copia H.264 de hasta 1080p y
  30 FPS en la caché del usuario, sin modificar el archivo original.
- La configuración y la caché se guardan localmente en tu cuenta de Windows.

MP4/H.264 es el formato recomendado para fondos largos. La conversión de
archivos incompatibles, incluido GIF en algunos equipos, puede aumentar
temporalmente el uso de recursos.

## Instalar y usar

1. Descarga y ejecuta `LumaWallpaper-Setup-1.0.0-x64.exe`.
2. Abre Luma Wallpaper desde el acceso directo creado por el instalador.
3. Selecciona un archivo `.gif` o `.mp4` y pulsa **Aplicar al escritorio**.

La ventana se puede cerrar sin salir de Luma: la aplicación queda en el área de
notificaciones. Haz clic derecho en el icono para volver a abrirla o salir.
La opción **Iniciar con Windows** controla el inicio automático.

## Crear el instalador

Necesitas el SDK de .NET 10 y Inno Setup 6. Desde PowerShell, ejecuta:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build-installer.ps1
```

El instalador se genera en `dist\LumaWallpaper-Setup-1.0.0-x64.exe`. La
publicación es autónoma, así que los equipos de destino no necesitan instalar
.NET por separado. Para iniciar el proyecto durante el desarrollo, usa
`Iniciar Luma Wallpaper.bat`.

## Compatibilidad y licencias

Luma está hecho para Windows 10 y 11 x64. DirectComposition se usa en los
escritorios modernos de Windows 11; en las demás versiones se conserva el
reproductor de Windows.

La conversión usa FFmpeg, distribuido bajo LGPL, y Media Foundation H.264 de
Windows. Los avisos y licencias de terceros se incluyen en el instalador y en
`LumaWallpaper\ffmpeg\licenses`. El código propio de Luma todavía no tiene una
licencia de reutilización: no se concede permiso explícito para copiarlo o
redistribuirlo.

## Ayuda

¿Encontraste un problema o tienes una sugerencia? [Abre una incidencia en
GitHub](https://github.com/jesus-mora-z/LumaWallpaper/issues).

---

[English version](README.en.md)
