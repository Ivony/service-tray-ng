using System.Globalization;

namespace service_tray_ng;

/// <summary>
/// Lightweight UI string table. Keyed strings are resolved against the current UI
/// culture and fall back to English when a translation is missing.
/// </summary>
public static class Strings
{
    private const string DefaultLanguage = "en";

    /// <summary>Returns the localized string for <paramref name="key"/>, falling back to English.</summary>
    public static string Get(string key)
    {
        var culture = CultureInfo.CurrentUICulture;
        if (Catalog.TryGetValue(GetLanguageCode(culture), out var table)
            && table.TryGetValue(key, out var text))
        {
            return text;
        }
        return En.TryGetValue(key, out var fallback) ? fallback : key;
    }

    /// <summary>Resolves the catalog language for a culture, mapping variants (zh-CN, zh-TW, pt-BR…) to a table key.</summary>
    internal static string GetLanguageCode(CultureInfo culture)
    {
        if (culture.IsNeutralCulture)
        {
            return Catalog.ContainsKey(culture.Name) ? culture.Name : DefaultLanguage;
        }
        return Catalog.ContainsKey(culture.TwoLetterISOLanguageName)
            ? culture.TwoLetterISOLanguageName
            : DefaultLanguage;
    }

    // NOTE: Catalog is declared at the bottom of the class, AFTER the per-language
    // tables, so the static field initializer sees them already assigned.

    private static readonly IReadOnlyDictionary<string, string> En = new Dictionary<string, string>
    {
        ["App.Title"] = "Service Tray",
        ["App.AlreadyRunning.Message"] = "Service Tray is already running in the notification area.",

        ["Menu.Status.Stopped"] = "Status: Stopped",
        ["Menu.Status.Error"] = "Status: Error",
        ["Menu.Status.Starting"] = "Status: Starting...",
        ["Menu.Status.Stopping"] = "Status: Stopping...",
        ["Menu.Status.Running"] = "Running on {0}:{1}",

        ["Menu.Start"] = "Start",
        ["Menu.Stop"] = "Stop",
        ["Menu.Restart"] = "Restart",
        ["Menu.Exit"] = "Exit",
        ["Menu.StartOnLogin"] = "Start tray on login",
        ["Menu.StartServiceOnLaunch"] = "Start service on launch",
        ["Menu.Port"] = "Port: {0}",
        ["Menu.AutoSwitchPort"] = "Auto-switch port when occupied",
        ["Menu.OpenLogFolder"] = "Open log folder",
        ["Menu.OpenConfig"] = "Open config",

        ["Balloon.Running"] = "Service running on {0}:{1}",
        ["Balloon.Stopped"] = "Service stopped",
        ["Balloon.Error"] = "Service failed to start. Open the log folder for details.",
        ["Balloon.StateChanged"] = "Service state changed.",
        ["Balloon.NotRunning"] = "Service is not running.",

        ["Dialog.ChangePort"] = "Change server port",
        ["Dialog.ServerPort"] = "Server port:",
        ["Dialog.OK"] = "OK",
        ["Dialog.Cancel"] = "Cancel",

        ["ServiceName.OpenCode"] = "OpenCode Service",
        ["ServiceName.Dsh"] = "Dsh Service",
    };

    private static readonly IReadOnlyDictionary<string, string> Zh = new Dictionary<string, string>
    {
        ["App.Title"] = "服务托盘",
        ["App.AlreadyRunning.Message"] = "服务托盘已在通知区域运行。",

        ["Menu.Status.Stopped"] = "状态：已停止",
        ["Menu.Status.Error"] = "状态：错误",
        ["Menu.Status.Starting"] = "状态：启动中...",
        ["Menu.Status.Stopping"] = "状态：停止中...",
        ["Menu.Status.Running"] = "运行于 {0}:{1}",

        ["Menu.Start"] = "启动",
        ["Menu.Stop"] = "停止",
        ["Menu.Restart"] = "重启",
        ["Menu.Exit"] = "退出",
        ["Menu.StartOnLogin"] = "登录时启动托盘",
        ["Menu.StartServiceOnLaunch"] = "托盘启动时运行服务",
        ["Menu.Port"] = "端口：{0}",
        ["Menu.AutoSwitchPort"] = "端口被占用时自动切换",
        ["Menu.OpenLogFolder"] = "打开日志目录",
        ["Menu.OpenConfig"] = "打开配置",

        ["Balloon.Running"] = "服务运行于 {0}:{1}",
        ["Balloon.Stopped"] = "服务已停止",
        ["Balloon.Error"] = "服务启动失败。请打开日志目录查看详情。",
        ["Balloon.StateChanged"] = "服务状态已变更。",
        ["Balloon.NotRunning"] = "服务未运行。",

        ["Dialog.ChangePort"] = "修改服务端口",
        ["Dialog.ServerPort"] = "服务端口：",
        ["Dialog.OK"] = "确定",
        ["Dialog.Cancel"] = "取消",

        ["ServiceName.OpenCode"] = "OpenCode 服务",
        ["ServiceName.Dsh"] = "Dsh 服务",
    };

    private static readonly IReadOnlyDictionary<string, string> Ja = new Dictionary<string, string>
    {
        ["App.Title"] = "サービス トレイ",
        ["App.AlreadyRunning.Message"] = "サービス トレイは通知領域で既に実行されています。",

        ["Menu.Status.Stopped"] = "ステータス: 停止",
        ["Menu.Status.Error"] = "ステータス: エラー",
        ["Menu.Status.Starting"] = "ステータス: 起動中...",
        ["Menu.Status.Stopping"] = "ステータス: 停止中...",
        ["Menu.Status.Running"] = "{0}:{1} で実行中",

        ["Menu.Start"] = "開始",
        ["Menu.Stop"] = "停止",
        ["Menu.Restart"] = "再起動",
        ["Menu.Exit"] = "終了",
        ["Menu.StartOnLogin"] = "ログイン時にトレイを起動",
        ["Menu.StartServiceOnLaunch"] = "起動時にサービスを開始",
        ["Menu.Port"] = "ポート: {0}",
        ["Menu.AutoSwitchPort"] = "ポートが使用中なら自動切替",
        ["Menu.OpenLogFolder"] = "ログフォルダを開く",
        ["Menu.OpenConfig"] = "設定を開く",

        ["Balloon.Running"] = "{0}:{1} でサービスを実行中",
        ["Balloon.Stopped"] = "サービスが停止しました",
        ["Balloon.Error"] = "サービスを開始できませんでした。ログフォルダを確認してください。",
        ["Balloon.StateChanged"] = "サービス状態が変更されました。",
        ["Balloon.NotRunning"] = "サービスが実行されていません。",

        ["Dialog.ChangePort"] = "ポートを変更",
        ["Dialog.ServerPort"] = "サーバーポート:",
        ["Dialog.OK"] = "OK",
        ["Dialog.Cancel"] = "キャンセル",

        ["ServiceName.OpenCode"] = "OpenCode サービス",
        ["ServiceName.Dsh"] = "Dsh サービス",
    };

    private static readonly IReadOnlyDictionary<string, string> Ko = new Dictionary<string, string>
    {
        ["App.Title"] = "서비스 트레이",
        ["App.AlreadyRunning.Message"] = "서비스 트레이가 알림 영역에서 이미 실행 중입니다.",

        ["Menu.Status.Stopped"] = "상태: 중지됨",
        ["Menu.Status.Error"] = "상태: 오류",
        ["Menu.Status.Starting"] = "상태: 시작 중...",
        ["Menu.Status.Stopping"] = "상태: 중지 중...",
        ["Menu.Status.Running"] = "{0}:{1}에서 실행 중",

        ["Menu.Start"] = "시작",
        ["Menu.Stop"] = "중지",
        ["Menu.Restart"] = "다시 시작",
        ["Menu.Exit"] = "종료",
        ["Menu.StartOnLogin"] = "로그인 시 트레이 시작",
        ["Menu.StartServiceOnLaunch"] = "실행 시 서비스 시작",
        ["Menu.Port"] = "포트: {0}",
        ["Menu.AutoSwitchPort"] = "포트 사용 중이면 자동 전환",
        ["Menu.OpenLogFolder"] = "로그 폴더 열기",
        ["Menu.OpenConfig"] = "설정 열기",

        ["Balloon.Running"] = "{0}:{1}에서 서비스 실행 중",
        ["Balloon.Stopped"] = "서비스가 중지되었습니다",
        ["Balloon.Error"] = "서비스를 시작하지 못했습니다. 로그 폴더를 확인하세요.",
        ["Balloon.StateChanged"] = "서비스 상태가 변경되었습니다.",
        ["Balloon.NotRunning"] = "서비스가 실행 중이 아닙니다.",

        ["Dialog.ChangePort"] = "포트 변경",
        ["Dialog.ServerPort"] = "서버 포트:",
        ["Dialog.OK"] = "확인",
        ["Dialog.Cancel"] = "취소",

        ["ServiceName.OpenCode"] = "OpenCode 서비스",
        ["ServiceName.Dsh"] = "Dsh 서비스",
    };

    private static readonly IReadOnlyDictionary<string, string> Fr = new Dictionary<string, string>
    {
        ["App.Title"] = "Service Tray",
        ["App.AlreadyRunning.Message"] = "Service Tray est déjà en cours d'exécution dans la zone de notification.",

        ["Menu.Status.Stopped"] = "État : arrêté",
        ["Menu.Status.Error"] = "État : erreur",
        ["Menu.Status.Starting"] = "État : démarrage...",
        ["Menu.Status.Stopping"] = "État : arrêt...",
        ["Menu.Status.Running"] = "En cours sur {0}:{1}",

        ["Menu.Start"] = "Démarrer",
        ["Menu.Stop"] = "Arrêter",
        ["Menu.Restart"] = "Redémarrer",
        ["Menu.Exit"] = "Quitter",
        ["Menu.StartOnLogin"] = "Lancer le tray à la connexion",
        ["Menu.StartServiceOnLaunch"] = "Démarrer le service au lancement",
        ["Menu.Port"] = "Port : {0}",
        ["Menu.AutoSwitchPort"] = "Changer de port automatiquement s'il est occupé",
        ["Menu.OpenLogFolder"] = "Ouvrir le dossier des journaux",
        ["Menu.OpenConfig"] = "Ouvrir la configuration",

        ["Balloon.Running"] = "Service en cours sur {0}:{1}",
        ["Balloon.Stopped"] = "Service arrêté",
        ["Balloon.Error"] = "Échec du démarrage du service. Consultez le dossier des journaux.",
        ["Balloon.StateChanged"] = "L'état du service a changé.",
        ["Balloon.NotRunning"] = "Le service n'est pas en cours d'exécution.",

        ["Dialog.ChangePort"] = "Modifier le port",
        ["Dialog.ServerPort"] = "Port du serveur :",
        ["Dialog.OK"] = "OK",
        ["Dialog.Cancel"] = "Annuler",

        ["ServiceName.OpenCode"] = "Service OpenCode",
        ["ServiceName.Dsh"] = "Service Dsh",
    };

    private static readonly IReadOnlyDictionary<string, string> De = new Dictionary<string, string>
    {
        ["App.Title"] = "Service Tray",
        ["App.AlreadyRunning.Message"] = "Service Tray wird bereits im Benachrichtigungsbereich ausgeführt.",

        ["Menu.Status.Stopped"] = "Status: Gestoppt",
        ["Menu.Status.Error"] = "Status: Fehler",
        ["Menu.Status.Starting"] = "Status: Startet...",
        ["Menu.Status.Stopping"] = "Status: Stoppt...",
        ["Menu.Status.Running"] = "Läuft auf {0}:{1}",

        ["Menu.Start"] = "Start",
        ["Menu.Stop"] = "Stopp",
        ["Menu.Restart"] = "Neu starten",
        ["Menu.Exit"] = "Beenden",
        ["Menu.StartOnLogin"] = "Tray bei Anmeldung starten",
        ["Menu.StartServiceOnLaunch"] = "Dienst beim Start ausführen",
        ["Menu.Port"] = "Port: {0}",
        ["Menu.AutoSwitchPort"] = "Port bei Belegung automatisch wechseln",
        ["Menu.OpenLogFolder"] = "Protokollordner öffnen",
        ["Menu.OpenConfig"] = "Konfiguration öffnen",

        ["Balloon.Running"] = "Dienst läuft auf {0}:{1}",
        ["Balloon.Stopped"] = "Dienst gestoppt",
        ["Balloon.Error"] = "Dienst konnte nicht gestartet werden. Siehe Protokollordner.",
        ["Balloon.StateChanged"] = "Dienststatus wurde geändert.",
        ["Balloon.NotRunning"] = "Der Dienst läuft nicht.",

        ["Dialog.ChangePort"] = "Port ändern",
        ["Dialog.ServerPort"] = "Server-Port:",
        ["Dialog.OK"] = "OK",
        ["Dialog.Cancel"] = "Abbrechen",

        ["ServiceName.OpenCode"] = "OpenCode-Dienst",
        ["ServiceName.Dsh"] = "Dsh-Dienst",
    };

    private static readonly IReadOnlyDictionary<string, string> Es = new Dictionary<string, string>
    {
        ["App.Title"] = "Service Tray",
        ["App.AlreadyRunning.Message"] = "Service Tray ya se está ejecutando en el área de notificación.",

        ["Menu.Status.Stopped"] = "Estado: detenido",
        ["Menu.Status.Error"] = "Estado: error",
        ["Menu.Status.Starting"] = "Estado: iniciando...",
        ["Menu.Status.Stopping"] = "Estado: deteniendo...",
        ["Menu.Status.Running"] = "Ejecutándose en {0}:{1}",

        ["Menu.Start"] = "Iniciar",
        ["Menu.Stop"] = "Detener",
        ["Menu.Restart"] = "Reiniciar",
        ["Menu.Exit"] = "Salir",
        ["Menu.StartOnLogin"] = "Iniciar bandeja al iniciar sesión",
        ["Menu.StartServiceOnLaunch"] = "Iniciar servicio al arrancar",
        ["Menu.Port"] = "Puerto: {0}",
        ["Menu.AutoSwitchPort"] = "Cambiar puerto automáticamente si está ocupado",
        ["Menu.OpenLogFolder"] = "Abrir carpeta de registros",
        ["Menu.OpenConfig"] = "Abrir configuración",

        ["Balloon.Running"] = "Servicio ejecutándose en {0}:{1}",
        ["Balloon.Stopped"] = "Servicio detenido",
        ["Balloon.Error"] = "No se pudo iniciar el servicio. Consulte la carpeta de registros.",
        ["Balloon.StateChanged"] = "El estado del servicio ha cambiado.",
        ["Balloon.NotRunning"] = "El servicio no se está ejecutando.",

        ["Dialog.ChangePort"] = "Cambiar puerto",
        ["Dialog.ServerPort"] = "Puerto del servidor:",
        ["Dialog.OK"] = "Aceptar",
        ["Dialog.Cancel"] = "Cancelar",

        ["ServiceName.OpenCode"] = "Servicio OpenCode",
        ["ServiceName.Dsh"] = "Servicio Dsh",
    };

    private static readonly IReadOnlyDictionary<string, string> It = new Dictionary<string, string>
    {
        ["App.Title"] = "Service Tray",
        ["App.AlreadyRunning.Message"] = "Service Tray è già in esecuzione nell'area di notifica.",

        ["Menu.Status.Stopped"] = "Stato: fermo",
        ["Menu.Status.Error"] = "Stato: errore",
        ["Menu.Status.Starting"] = "Stato: avvio...",
        ["Menu.Status.Stopping"] = "Stato: arresto...",
        ["Menu.Status.Running"] = "In esecuzione su {0}:{1}",

        ["Menu.Start"] = "Avvia",
        ["Menu.Stop"] = "Ferma",
        ["Menu.Restart"] = "Riavvia",
        ["Menu.Exit"] = "Esci",
        ["Menu.StartOnLogin"] = "Avvia la tray all'accesso",
        ["Menu.StartServiceOnLaunch"] = "Avvia il servizio all'avvio",
        ["Menu.Port"] = "Porta: {0}",
        ["Menu.AutoSwitchPort"] = "Cambia porta se occupata",
        ["Menu.OpenLogFolder"] = "Apri cartella log",
        ["Menu.OpenConfig"] = "Apri configurazione",

        ["Balloon.Running"] = "Servizio in esecuzione su {0}:{1}",
        ["Balloon.Stopped"] = "Servizio fermato",
        ["Balloon.Error"] = "Impossibile avviare il servizio. Controllare la cartella log.",
        ["Balloon.StateChanged"] = "Lo stato del servizio è cambiato.",
        ["Balloon.NotRunning"] = "Il servizio non è in esecuzione.",

        ["Dialog.ChangePort"] = "Cambia porta",
        ["Dialog.ServerPort"] = "Porta del server:",
        ["Dialog.OK"] = "OK",
        ["Dialog.Cancel"] = "Annulla",

        ["ServiceName.OpenCode"] = "Servizio OpenCode",
        ["ServiceName.Dsh"] = "Servizio Dsh",
    };

    private static readonly IReadOnlyDictionary<string, string> Pt = new Dictionary<string, string>
    {
        ["App.Title"] = "Service Tray",
        ["App.AlreadyRunning.Message"] = "O Service Tray já está em execução na área de notificação.",

        ["Menu.Status.Stopped"] = "Estado: parado",
        ["Menu.Status.Error"] = "Estado: erro",
        ["Menu.Status.Starting"] = "Estado: iniciando...",
        ["Menu.Status.Stopping"] = "Estado: parando...",
        ["Menu.Status.Running"] = "Em execução em {0}:{1}",

        ["Menu.Start"] = "Iniciar",
        ["Menu.Stop"] = "Parar",
        ["Menu.Restart"] = "Reiniciar",
        ["Menu.Exit"] = "Sair",
        ["Menu.StartOnLogin"] = "Iniciar bandeja ao fazer login",
        ["Menu.StartServiceOnLaunch"] = "Iniciar serviço ao iniciar",
        ["Menu.Port"] = "Porta: {0}",
        ["Menu.AutoSwitchPort"] = "Trocar porta se ocupada",
        ["Menu.OpenLogFolder"] = "Abrir pasta de logs",
        ["Menu.OpenConfig"] = "Abrir configuração",

        ["Balloon.Running"] = "Serviço em execução em {0}:{1}",
        ["Balloon.Stopped"] = "Serviço parado",
        ["Balloon.Error"] = "Falha ao iniciar o serviço. Consulte a pasta de logs.",
        ["Balloon.StateChanged"] = "O estado do serviço mudou.",
        ["Balloon.NotRunning"] = "O serviço não está em execução.",

        ["Dialog.ChangePort"] = "Alterar porta",
        ["Dialog.ServerPort"] = "Porta do servidor:",
        ["Dialog.OK"] = "OK",
        ["Dialog.Cancel"] = "Cancelar",

        ["ServiceName.OpenCode"] = "Serviço OpenCode",
        ["ServiceName.Dsh"] = "Serviço Dsh",
    };

    private static readonly IReadOnlyDictionary<string, string> Ru = new Dictionary<string, string>
    {
        ["App.Title"] = "Service Tray",
        ["App.AlreadyRunning.Message"] = "Service Tray уже запущен в области уведомлений.",

        ["Menu.Status.Stopped"] = "Состояние: остановлен",
        ["Menu.Status.Error"] = "Состояние: ошибка",
        ["Menu.Status.Starting"] = "Состояние: запуск...",
        ["Menu.Status.Stopping"] = "Состояние: остановка...",
        ["Menu.Status.Running"] = "Работает на {0}:{1}",

        ["Menu.Start"] = "Запустить",
        ["Menu.Stop"] = "Остановить",
        ["Menu.Restart"] = "Перезапустить",
        ["Menu.Exit"] = "Выход",
        ["Menu.StartOnLogin"] = "Запускать трей при входе в систему",
        ["Menu.StartServiceOnLaunch"] = "Запускать службу при старте",
        ["Menu.Port"] = "Порт: {0}",
        ["Menu.AutoSwitchPort"] = "Автоматически менять порт, если он занят",
        ["Menu.OpenLogFolder"] = "Открыть папку журналов",
        ["Menu.OpenConfig"] = "Открыть конфигурацию",

        ["Balloon.Running"] = "Служба работает на {0}:{1}",
        ["Balloon.Stopped"] = "Служба остановлена",
        ["Balloon.Error"] = "Не удалось запустить службу. Проверьте папку журналов.",
        ["Balloon.StateChanged"] = "Состояние службы изменилось.",
        ["Balloon.NotRunning"] = "Служба не запущена.",

        ["Dialog.ChangePort"] = "Изменить порт",
        ["Dialog.ServerPort"] = "Порт сервера:",
        ["Dialog.OK"] = "ОК",
        ["Dialog.Cancel"] = "Отмена",

        ["ServiceName.OpenCode"] = "Служба OpenCode",
        ["ServiceName.Dsh"] = "Служба Dsh",
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Catalog =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["en"] = En,
            ["zh"] = Zh,
            ["ja"] = Ja,
            ["ko"] = Ko,
            ["fr"] = Fr,
            ["de"] = De,
            ["es"] = Es,
            ["it"] = It,
            ["pt"] = Pt,
            ["ru"] = Ru,
        };
}
