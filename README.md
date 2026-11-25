# Luma Wallpaper

Aplicacion nativa para Windows que coloca un fondo GIF o MP4 detras de los
iconos del escritorio. Los videos se reproducen sin audio y en bucle. La
aplicacion permanece en el area de notificaciones y puede restaurar el ultimo
fondo al iniciar sesion en Windows.

## Instalar y compartir

El instalador para Windows x64 se genera con
`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build-installer.ps1`
y queda en `dist\LumaWallpaper-Setup-1.0.0-x64.exe`. La publicacion incluye
.NET, asi que el equipo de destino no necesita instalar el runtime por
separado.

Para generar el instalador desde el codigo se necesita el SDK de .NET 10 y
Inno Setup 6. La instalacion se realiza en la carpeta del usuario actual, sin
permisos de administrador. El instalador crea accesos directos en Inicio y en
el escritorio; al terminar, ofrece iniciar Luma.

El instalador aun no esta firmado digitalmente. Windows puede mostrar una
advertencia de SmartScreen al instalarlo desde Internet.

## Iniciar

Tras instalar, abre Luma Wallpaper desde el acceso directo. Para desarrollo,
tambien puedes usar `Iniciar Luma Wallpaper.bat`, que ejecuta la compilacion
local.

Selecciona un archivo `.gif` o `.mp4`, pulsa **Aplicar al escritorio** y usa
los controles para pausar o quitar el fondo. Al cerrar la ventana, la
aplicacion y el fondo siguen activos en el area de notificaciones. Haz clic
derecho en el icono de Luma para abrir la ventana o salir por completo.

El inicio automatico con Windows se activa la primera vez y se puede desactivar
con la casilla **Iniciar con Windows**. El ultimo fondo aplicado se guarda y se
restaura al iniciar sesion. La configuracion es solo para el usuario actual y
se puede cambiar sin permisos de administrador.

## Consumo de recursos

MP4/H.264 es la opcion recomendada para fondos largos. La reproduccion usa
el decodificador por hardware de Windows y DirectComposition en equipos con el
escritorio moderno de Windows 11. En las versiones anteriores se conserva el
reproductor de Windows. Si el decodificador no puede abrir un video, Luma crea
una copia H.264 a 1080p y 30 FPS en la caché del usuario, sin modificar el
original ni su audio. Esa copia se reutiliza al iniciar Windows. Los GIF se
leen con el decodificador WIC en las versiones anteriores y se convierten a
H.264 cuando hace falta reproducirlos con DirectComposition. La conversion
puede aumentar temporalmente el consumo mientras se procesa el archivo.

La conversion usa FFmpeg con licencia LGPL y Media Foundation H.264 de Windows;
el resultado se deja sin audio y se codifica con una tasa adecuada para fondos
1080p. Los avisos y licencias de terceros se guardan en
`LumaWallpaper\ffmpeg\licenses` junto al ejecutable.
