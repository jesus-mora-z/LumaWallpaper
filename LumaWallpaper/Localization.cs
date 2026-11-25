using System.Globalization;

namespace LumaWallpaper;

internal sealed record LanguageOption(string Code, string Name);

internal static class Localization
{
    public static IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("es", "Español"),
        new("en", "English"),
        new("pt-BR", "Português (Brasil)"),
        new("fr", "Français"),
        new("de", "Deutsch"),
        new("it", "Italiano"),
        new("ja", "日本語")
    ];

    private static readonly Dictionary<string, int> LanguageIndexes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["es"] = 0,
        ["en"] = 1,
        ["pt-BR"] = 2,
        ["fr"] = 3,
        ["de"] = 4,
        ["it"] = 5,
        ["ja"] = 6
    };

    private static readonly IReadOnlyDictionary<string, string[]> Strings =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Tagline"] = ["Tu escritorio, en movimiento.", "Your desktop, in motion.", "Seu desktop em movimento.", "Votre bureau, en mouvement.", "Dein Desktop in Bewegung.", "Il tuo desktop, in movimento.", "動きのあるデスクトップ。"],
            ["Subtitle"] = ["Fondos GIF y MP4, reproducidos con las herramientas de Windows.", "GIF and MP4 wallpapers, powered by Windows.", "Papéis de parede GIF e MP4, reproduzidos pelo Windows.", "Fonds d’écran GIF et MP4, lus avec les outils de Windows.", "GIF- und MP4-Hintergründe mit der Windows-Wiedergabe.", "Sfondi GIF e MP4 riprodotti con gli strumenti di Windows.", "Windows の機能で再生する GIF・MP4 壁紙。"],
            ["PreviewHeading"] = ["VISTA PREVIA", "PREVIEW", "PRÉVIA", "APERÇU", "VORSCHAU", "ANTEPRIMA", "プレビュー"],
            ["PreviewPlaceholder"] = ["Elige un GIF o MP4 para empezar", "Choose a GIF or MP4 to get started", "Escolha um GIF ou MP4 para começar", "Choisissez un GIF ou MP4 pour commencer", "Wähle ein GIF oder MP4 zum Starten", "Scegli una GIF o un MP4 per iniziare", "GIF または MP4 を選択してください"],
            ["CurrentWallpaper"] = ["Fondo actual", "Current wallpaper", "Papel de parede atual", "Fond d’écran actuel", "Aktueller Hintergrund", "Sfondo attuale", "現在の壁紙"],
            ["NoFileSelected"] = ["Ningún archivo seleccionado", "No file selected", "Nenhum arquivo selecionado", "Aucun fichier sélectionné", "Keine Datei ausgewählt", "Nessun file selezionato", "ファイルが選択されていません"],
            ["ConvertedFileName"] = ["{0} (H.264 1080p)", "{0} (H.264 1080p)", "{0} (H.264 1080p)", "{0} (H.264 1080p)", "{0} (H.264 1080p)", "{0} (H.264 1080p)", "{0} (H.264 1080p)"],
            ["ChooseFile"] = ["Elegir GIF o MP4", "Choose GIF or MP4", "Escolher GIF ou MP4", "Choisir un GIF ou MP4", "GIF oder MP4 auswählen", "Scegli GIF o MP4", "GIF / MP4 を選択"],
            ["Apply"] = ["Aplicar al escritorio", "Apply to desktop", "Aplicar à área de trabalho", "Appliquer au bureau", "Auf den Desktop anwenden", "Applica al desktop", "デスクトップに適用"],
            ["Pause"] = ["Pausar", "Pause", "Pausar", "Mettre en pause", "Pausieren", "Pausa", "一時停止"],
            ["Resume"] = ["Reanudar", "Resume", "Retomar", "Reprendre", "Fortsetzen", "Riprendi", "再開"],
            ["Remove"] = ["Quitar fondo", "Remove wallpaper", "Remover papel de parede", "Retirer le fond d’écran", "Hintergrund entfernen", "Rimuovi sfondo", "壁紙を解除"],
            ["AutoStart"] = ["Iniciar con Windows", "Start with Windows", "Iniciar com o Windows", "Démarrer avec Windows", "Mit Windows starten", "Avvia con Windows", "Windows 起動時に開始"],
            ["Ready"] = ["Listo para empezar", "Ready to get started", "Pronto para começar", "Prêt à commencer", "Bereit zum Starten", "Pronto per iniziare", "準備完了"],
            ["Tip"] = ["Consejo: para reducir el consumo, usa videos MP4/H.264 optimizados para la resolución de tu pantalla.", "Tip: use MP4/H.264 videos optimized for your screen resolution to reduce resource usage.", "Dica: use vídeos MP4/H.264 otimizados para a resolução da tela e reduza o consumo.", "Conseil : utilisez des vidéos MP4/H.264 adaptées à la résolution de votre écran pour réduire la consommation.", "Tipp: Verwende für geringeren Ressourcenverbrauch MP4/H.264-Videos in deiner Bildschirmauflösung.", "Suggerimento: usa video MP4/H.264 ottimizzati per la risoluzione dello schermo per ridurre i consumi.", "ヒント: リソース消費を抑えるには、画面解像度に合わせた MP4/H.264 動画を使用してください。"],
            ["Language"] = ["Idioma", "Language", "Idioma", "Langue", "Sprache", "Lingua", "言語"],
            ["TrayOpen"] = ["Abrir Luma Wallpaper", "Open Luma Wallpaper", "Abrir Luma Wallpaper", "Ouvrir Luma Wallpaper", "Luma Wallpaper öffnen", "Apri Luma Wallpaper", "Luma Wallpaper を開く"],
            ["TrayExit"] = ["Salir", "Exit", "Sair", "Quitter", "Beenden", "Esci", "終了"],
            ["TrayIconText"] = ["Luma Wallpaper", "Luma Wallpaper", "Luma Wallpaper", "Luma Wallpaper", "Luma Wallpaper", "Luma Wallpaper", "Luma Wallpaper"],
            ["StatusSettingsFailed"] = ["No se pudo configurar el inicio automático.", "Could not configure startup.", "Não foi possível configurar a inicialização.", "Impossible de configurer le démarrage.", "Autostart konnte nicht eingerichtet werden.", "Impossibile configurare l’avvio automatico.", "自動起動を設定できませんでした。"],
            ["DialogStartupSettings"] = ["Configuración de inicio", "Startup settings", "Configurações de inicialização", "Paramètres de démarrage", "Starteinstellungen", "Impostazioni di avvio", "起動設定"],
            ["StatusBackgroundWaiting"] = ["En segundo plano. Elige un fondo para aplicarlo.", "Running in the background. Choose a wallpaper to apply.", "Em segundo plano. Escolha um papel de parede para aplicar.", "En arrière-plan. Choisissez un fond d’écran à appliquer.", "Läuft im Hintergrund. Wähle einen Hintergrund aus.", "In background. Scegli uno sfondo da applicare.", "バックグラウンドで実行中です。壁紙を選択してください。"],
            ["StatusSavedMissing"] = ["El fondo guardado ya no existe. Elige otro desde la bandeja.", "The saved wallpaper no longer exists. Choose another from the tray.", "O papel de parede salvo não existe mais. Escolha outro pela bandeja.", "Le fond d’écran enregistré n’existe plus. Choisissez-en un autre depuis la zone de notification.", "Der gespeicherte Hintergrund existiert nicht mehr. Wähle über das Infobereichssymbol einen anderen.", "Lo sfondo salvato non esiste più. Scegline un altro dall’area di notifica.", "保存した壁紙が見つかりません。通知領域から別の壁紙を選択してください。"],
            ["StatusRestoreFailed"] = ["No se pudo restaurar el fondo guardado.", "Could not restore the saved wallpaper.", "Não foi possível restaurar o papel de parede salvo.", "Impossible de restaurer le fond d’écran enregistré.", "Der gespeicherte Hintergrund konnte nicht wiederhergestellt werden.", "Impossibile ripristinare lo sfondo salvato.", "保存した壁紙を復元できませんでした。"],
            ["StatusAutoEnabled"] = ["Luma Wallpaper se iniciará con Windows.", "Luma Wallpaper will start with Windows.", "O Luma Wallpaper será iniciado com o Windows.", "Luma Wallpaper démarrera avec Windows.", "Luma Wallpaper wird mit Windows gestartet.", "Luma Wallpaper si avvierà con Windows.", "Luma Wallpaper は Windows と同時に起動します。"],
            ["StatusAutoDisabled"] = ["El inicio automático está desactivado.", "Automatic startup is disabled.", "A inicialização automática está desativada.", "Le démarrage automatique est désactivé.", "Der automatische Start ist deaktiviert.", "L’avvio automatico è disattivato.", "自動起動は無効です。"],
            ["StatusAutoFailed"] = ["No se pudo cambiar el inicio automático.", "Could not change automatic startup.", "Não foi possível alterar a inicialização automática.", "Impossible de modifier le démarrage automatique.", "Der automatische Start konnte nicht geändert werden.", "Impossibile modificare l’avvio automatico.", "自動起動を変更できませんでした。"],
            ["StatusPreviewReady"] = ["Fondo listo para aplicar", "Wallpaper ready to apply", "Papel de parede pronto para aplicar", "Fond d’écran prêt à appliquer", "Hintergrund kann angewendet werden", "Sfondo pronto da applicare", "壁紙を適用できます"],
            ["StatusPreviewChanged"] = ["La vista previa cambió; el fondo activo sigue igual hasta aplicar.", "The preview changed; the active wallpaper stays the same until applied.", "A prévia mudou; o papel de parede atual permanece até aplicar.", "L’aperçu a changé ; le fond actif reste inchangé jusqu’à l’application.", "Die Vorschau wurde geändert; der aktive Hintergrund bleibt bis zur Anwendung unverändert.", "L’anteprima è cambiata; lo sfondo attivo resta invariato finché non lo applichi.", "プレビューが変わりました。適用するまで現在の壁紙は変わりません。"],
            ["StatusOpenFailed"] = ["No se pudo abrir el archivo seleccionado.", "Could not open the selected file.", "Não foi possível abrir o arquivo selecionado.", "Impossible d’ouvrir le fichier sélectionné.", "Die ausgewählte Datei konnte nicht geöffnet werden.", "Impossibile aprire il file selezionato.", "選択したファイルを開けませんでした。"],
            ["DialogOpenFailed"] = ["No se pudo abrir el fondo", "Could not open wallpaper", "Não foi possível abrir o papel de parede", "Impossible d’ouvrir le fond d’écran", "Hintergrund konnte nicht geöffnet werden", "Impossibile aprire lo sfondo", "壁紙を開けませんでした"],
            ["StatusChooseFirst"] = ["Primero elige un archivo GIF o MP4.", "Choose a GIF or MP4 file first.", "Escolha um arquivo GIF ou MP4 primeiro.", "Choisissez d’abord un fichier GIF ou MP4.", "Wähle zuerst eine GIF- oder MP4-Datei aus.", "Scegli prima un file GIF o MP4.", "先に GIF または MP4 ファイルを選択してください。"],
            ["DialogChooseWallpaper"] = ["Elige un fondo", "Choose a wallpaper", "Escolha um papel de parede", "Choisir un fond d’écran", "Hintergrund auswählen", "Scegli uno sfondo", "壁紙を選択"],
            ["StatusApplying"] = ["Aplicando fondo · {0}", "Applying wallpaper · {0}", "Aplicando papel de parede · {0}", "Application du fond d’écran · {0}", "Hintergrund wird angewendet · {0}", "Applicazione dello sfondo · {0}", "壁紙を適用中 · {0}"],
            ["StatusApplyFailed"] = ["No se pudo aplicar el fondo.", "Could not apply the wallpaper.", "Não foi possível aplicar o papel de parede.", "Impossible d’appliquer le fond d’écran.", "Der Hintergrund konnte nicht angewendet werden.", "Impossibile applicare lo sfondo.", "壁紙を適用できませんでした。"],
            ["DialogApplyFailed"] = ["No se pudo aplicar el fondo", "Could not apply wallpaper", "Não foi possível aplicar o papel de parede", "Impossible d’appliquer le fond d’écran", "Hintergrund konnte nicht angewendet werden", "Impossibile applicare lo sfondo", "壁紙を適用できませんでした"],
            ["StatusWallpaperVisible"] = ["Fondo visible · {0}", "Wallpaper visible · {0}", "Papel de parede visível · {0}", "Fond d’écran visible · {0}", "Hintergrund sichtbar · {0}", "Sfondo visibile · {0}", "壁紙を表示中 · {0}"],
            ["StatusPauseFirst"] = ["Aplica un fondo antes de pausarlo.", "Apply a wallpaper before pausing it.", "Aplique um papel de parede antes de pausá-lo.", "Appliquez un fond d’écran avant de le mettre en pause.", "Wende zuerst einen Hintergrund an, bevor du ihn pausierst.", "Applica uno sfondo prima di metterlo in pausa.", "一時停止する前に壁紙を適用してください。"],
            ["StatusWallpaperMoving"] = ["Fondo en movimiento", "Wallpaper is moving", "Papel de parede em movimento", "Le fond d’écran est animé", "Hintergrund bewegt sich", "Sfondo in movimento", "壁紙を再生中"],
            ["StatusWallpaperPaused"] = ["Fondo pausado", "Wallpaper paused", "Papel de parede pausado", "Fond d’écran en pause", "Hintergrund pausiert", "Sfondo in pausa", "壁紙を一時停止中"],
            ["StatusWallpaperRemoved"] = ["Fondo animado quitado", "Animated wallpaper removed", "Papel de parede animado removido", "Fond d’écran animé retiré", "Animierter Hintergrund entfernt", "Sfondo animato rimosso", "動く壁紙を解除しました"],
            ["StatusVideoReady"] = ["Video listo · {0}", "Video ready · {0}", "Vídeo pronto · {0}", "Vidéo prête · {0}", "Video bereit · {0}", "Video pronto · {0}", "動画の準備完了 · {0}"],
            ["StatusConversionReady"] = ["Conversión lista. El video se reproducirá en H.264.", "Conversion complete. The video will play in H.264.", "Conversão concluída. O vídeo será reproduzido em H.264.", "Conversion terminée. La vidéo sera lue en H.264.", "Konvertierung abgeschlossen. Das Video wird in H.264 wiedergegeben.", "Conversione completata. Il video verrà riprodotto in H.264.", "変換が完了しました。動画は H.264 で再生されます。"],
            ["StatusConverting"] = ["Convirtiendo {0} a H.264 1080p...", "Converting {0} to H.264 1080p...", "Convertendo {0} para H.264 1080p...", "Conversion de {0} en H.264 1080p...", "{0} wird in H.264 1080p konvertiert...", "Conversione di {0} in H.264 1080p...", "{0} を H.264 1080p に変換中..."],
            ["StatusConvertingProgress"] = ["Convirtiendo video a H.264 1080p... {0}", "Converting video to H.264 1080p... {0}", "Convertendo vídeo para H.264 1080p... {0}", "Conversion de la vidéo en H.264 1080p... {0}", "Video wird in H.264 1080p konvertiert... {0}", "Conversione del video in H.264 1080p... {0}", "動画を H.264 1080p に変換中... {0}"],
            ["StatusConversionFailed"] = ["No se pudo convertir este video.", "Could not convert this video.", "Não foi possível converter este vídeo.", "Impossible de convertir cette vidéo.", "Dieses Video konnte nicht konvertiert werden.", "Impossibile convertire questo video.", "この動画を変換できませんでした。"],
            ["DialogConversionFailed"] = ["No se pudo convertir el video", "Could not convert video", "Não foi possível converter o vídeo", "Impossible de convertir la vidéo", "Video konnte nicht konvertiert werden", "Impossibile convertire il video", "動画を変換できませんでした"],
            ["ConversionDetails"] = ["{0}\n\nLa conversión usa los códecs disponibles en Windows y crea una copia H.264 en la caché de Luma.", "{0}\n\nConversion uses the codecs available in Windows and creates an H.264 copy in Luma's cache.", "{0}\n\nA conversão usa os codecs disponíveis no Windows e cria uma cópia H.264 no cache do Luma.", "{0}\n\nLa conversion utilise les codecs disponibles dans Windows et crée une copie H.264 dans le cache de Luma.", "{0}\n\nDie Konvertierung verwendet verfügbare Windows-Codecs und erstellt eine H.264-Kopie im Luma-Cache.", "{0}\n\nLa conversione usa i codec disponibili in Windows e crea una copia H.264 nella cache di Luma.", "{0}\n\n変換には Windows で利用可能なコーデックを使用し、Luma のキャッシュに H.264 のコピーを作成します。"],
            ["BalloonConversionFailed"] = ["No se pudo convertir el último video. Abre Luma para ver los detalles.", "Could not convert the last video. Open Luma for details.", "Não foi possível converter o último vídeo. Abra o Luma para ver os detalhes.", "Impossible de convertir la dernière vidéo. Ouvrez Luma pour plus de détails.", "Das letzte Video konnte nicht konvertiert werden. Öffne Luma für Details.", "Impossibile convertire l’ultimo video. Apri Luma per i dettagli.", "最後の動画を変換できませんでした。詳細は Luma を開いてください。"],
            ["StatusPlaybackFailed"] = ["Windows no pudo reproducir este video.", "Windows could not play this video.", "O Windows não conseguiu reproduzir este vídeo.", "Windows n’a pas pu lire cette vidéo.", "Windows konnte dieses Video nicht wiedergeben.", "Windows non è riuscito a riprodurre questo video.", "Windows でこの動画を再生できませんでした。"],
            ["DialogPlaybackError"] = ["Error de reproducción", "Playback error", "Erro de reprodução", "Erreur de lecture", "Wiedergabefehler", "Errore di riproduzione", "再生エラー"],
            ["PlaybackDetails"] = ["{0}\n\nSi está codificado en HEVC/H.265, Luma intentará convertirlo automáticamente a H.264.", "{0}\n\nIf it is encoded in HEVC/H.265, Luma will try to convert it to H.264 automatically.", "{0}\n\nSe estiver codificado em HEVC/H.265, o Luma tentará convertê-lo automaticamente para H.264.", "{0}\n\nSi elle est encodée en HEVC/H.265, Luma tentera de la convertir automatiquement en H.264.", "{0}\n\nBei HEVC/H.265 versucht Luma automatisch, das Video in H.264 zu konvertieren.", "{0}\n\nSe è codificato in HEVC/H.265, Luma proverà a convertirlo automaticamente in H.264.", "{0}\n\nHEVC/H.265 でエンコードされている場合、Luma は H.264 への自動変換を試みます。"],
            ["StatusBackgroundTray"] = ["Luma Wallpaper sigue en segundo plano. Usa el icono de la bandeja para abrir o salir.", "Luma Wallpaper is still running in the background. Use the tray icon to open it or exit.", "O Luma Wallpaper continua em segundo plano. Use o ícone da bandeja para abrir ou sair.", "Luma Wallpaper continue en arrière-plan. Utilisez l’icône de la zone de notification pour ouvrir ou quitter.", "Luma Wallpaper läuft weiterhin im Hintergrund. Über das Infobereichssymbol öffnen oder beenden.", "Luma Wallpaper è ancora in background. Usa l’icona nell’area di notifica per aprire o uscire.", "Luma Wallpaper はバックグラウンドで実行中です。通知領域のアイコンから開くか終了してください。"],
            ["FileDialogAll"] = ["Fondos animados (*.gif;*.mp4)", "Animated wallpapers (*.gif;*.mp4)", "Papéis de parede animados (*.gif;*.mp4)", "Fonds d’écran animés (*.gif;*.mp4)", "Animierte Hintergründe (*.gif;*.mp4)", "Sfondi animati (*.gif;*.mp4)", "動く壁紙 (*.gif;*.mp4)"],
            ["FileDialogGif"] = ["GIF (*.gif)", "GIF (*.gif)", "GIF (*.gif)", "GIF (*.gif)", "GIF (*.gif)", "GIF (*.gif)", "GIF (*.gif)"],
            ["FileDialogMp4"] = ["Video MP4 (*.mp4)", "MP4 video (*.mp4)", "Vídeo MP4 (*.mp4)", "Vidéo MP4 (*.mp4)", "MP4-Video (*.mp4)", "Video MP4 (*.mp4)", "MP4 動画 (*.mp4)"],
            ["StatusFileTitle"] = ["Fondo animado", "Animated wallpaper", "Papel de parede animado", "Fond d’écran animé", "Animierter Hintergrund", "Sfondo animato", "動く壁紙"],
            ["StatusWinCannotPlay"] = ["Windows no pudo reproducir este video. Comprueba el formato del archivo.", "Windows could not play this video. Check the file format.", "O Windows não conseguiu reproduzir este vídeo. Verifique o formato do arquivo.", "Windows n’a pas pu lire cette vidéo. Vérifiez le format du fichier.", "Windows konnte dieses Video nicht wiedergeben. Überprüfe das Dateiformat.", "Windows non è riuscito a riprodurre questo video. Controlla il formato del file.", "Windows でこの動画を再生できませんでした。ファイル形式を確認してください。"],
            ["StatusRestartRequired"] = ["Cierra y vuelve a abrir Luma para aplicar el nuevo idioma.", "Close and reopen Luma to apply the new language.", "Feche e reabra o Luma para aplicar o novo idioma.", "Fermez puis rouvrez Luma pour appliquer la nouvelle langue.", "Schließe Luma und öffne es erneut, um die neue Sprache anzuwenden.", "Chiudi e riapri Luma per applicare la nuova lingua.", "新しい言語を適用するには Luma を再起動してください。"],
            ["StatusLanguageSaveFailed"] = ["No se pudo guardar el idioma seleccionado.", "Could not save the selected language.", "Não foi possível salvar o idioma selecionado.", "Impossible d’enregistrer la langue sélectionnée.", "Die ausgewählte Sprache konnte nicht gespeichert werden.", "Impossibile salvare la lingua selezionata.", "選択した言語を保存できませんでした。"],
            ["DialogLanguageSaveFailed"] = ["Error de idioma", "Language error", "Erro de idioma", "Erreur de langue", "Sprachfehler", "Errore lingua", "言語エラー"],
            ["StatusRunningAlready"] = ["Luma Wallpaper ya se está ejecutando. Usa el icono de la bandeja para abrirlo.", "Luma Wallpaper is already running. Use the tray icon to open it.", "O Luma Wallpaper já está em execução. Use o ícone da bandeja para abri-lo.", "Luma Wallpaper est déjà en cours d’exécution. Utilisez l’icône de la zone de notification pour l’ouvrir.", "Luma Wallpaper wird bereits ausgeführt. Öffne es über das Infobereichssymbol.", "Luma Wallpaper è già in esecuzione. Usa l’icona nell’area di notifica per aprirlo.", "Luma Wallpaper はすでに実行中です。通知領域のアイコンから開いてください。"]
        };

    private static string _language = "es";

    public static string Language => _language;

    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("es");

    public static string NormalizeLanguage(string? language) =>
        language is not null && LanguageIndexes.ContainsKey(language) ? Languages[LanguageIndexes[language]].Code : "es";

    public static void SetLanguage(string? language)
    {
        _language = NormalizeLanguage(language);
        Culture = CultureInfo.GetCultureInfo(_language);
    }

    public static string Get(string key, params object?[] arguments)
    {
        if (!Strings.TryGetValue(key, out var translations))
            throw new KeyNotFoundException($"Missing localization key: {key}");

        var index = LanguageIndexes[_language];
        var value = translations[index];
        return arguments.Length == 0 ? value : string.Format(Culture, value, arguments);
    }
}
