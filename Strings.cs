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

        ["Dialog.ExternalProcess.Title"] = "External process detected",
        ["Dialog.ExternalProcess.Message"] = "The {0} is already running on {1}:{2}. What would you like to do?",
        ["Dialog.ExternalProcess.Attach"] = "Take over existing process",
        ["Dialog.ExternalProcess.Kill"] = "Force kill all processes",
        ["Dialog.ExternalProcess.StartNew"] = "Start a new process",
        ["Dialog.ExternalProcess.AttachOption"] = "Take over existing process\r\n{0}",
        ["Dialog.ExternalProcess.KillOption"] = "Close all existing processes\r\n{0}",
        ["Dialog.ExternalProcess.StartNewOption"] = "Start a new process on another port",
        ["Dialog.ExternalProcess.ProcessDetail"] = "  {0} (PID {1}) listening on {2}",
        ["Dialog.ExternalProcess.NoProcessDetails"] = "  No process details available.",
        ["Dialog.ExternalProcess.NewPort"] = "New port:",

        ["Balloon.Attached"] = "Service taken over on {0}:{1}",
        ["Balloon.AttachFailed"] = "Could not take over the existing process.",

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

        ["Dialog.ExternalProcess.Title"] = "检测到外部进程",
        ["Dialog.ExternalProcess.Message"] = "{0} 已在 {1}:{2} 上运行。您想怎么处理？",
        ["Dialog.ExternalProcess.Attach"] = "接管现有进程",
        ["Dialog.ExternalProcess.Kill"] = "强行杀掉所有进程",
        ["Dialog.ExternalProcess.StartNew"] = "开启新的进程",
        ["Dialog.ExternalProcess.AttachOption"] = "接管现有进程\r\n{0}",
        ["Dialog.ExternalProcess.KillOption"] = "关闭所有现有进程\r\n{0}",
        ["Dialog.ExternalProcess.StartNewOption"] = "在其他端口开启新的进程",
        ["Dialog.ExternalProcess.ProcessDetail"] = "  {0}（PID {1}），监听 {2}",
        ["Dialog.ExternalProcess.NoProcessDetails"] = "  暂时无法获取进程详情。",
        ["Dialog.ExternalProcess.NewPort"] = "新端口：",

        ["Balloon.Attached"] = "已接管运行于 {0}:{1} 的服务",
        ["Balloon.AttachFailed"] = "无法接管现有进程。",

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

        ["Dialog.ExternalProcess.Title"] = "外部プロセスを検出しました",
        ["Dialog.ExternalProcess.Message"] = "{0} は既に {1}:{2} で実行されています。どうしますか？",
        ["Dialog.ExternalProcess.Attach"] = "既存プロセスを引き継ぐ",
        ["Dialog.ExternalProcess.Kill"] = "すべてのプロセスを強制終了",
        ["Dialog.ExternalProcess.StartNew"] = "新しいプロセスを開始",

        ["Balloon.Attached"] = "{0}:{1} のサービスを引き継ぎました",
        ["Balloon.AttachFailed"] = "既存プロセスを引き継げませんでした。",

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

        ["Dialog.ExternalProcess.Title"] = "외부 프로세스 감지됨",
        ["Dialog.ExternalProcess.Message"] = "{0}이(가) 이미 {1}:{2}에서 실행 중입니다. 어떻게 하시겠습니까?",
        ["Dialog.ExternalProcess.Attach"] = "기존 프로세스 인계",
        ["Dialog.ExternalProcess.Kill"] = "모든 프로세스 강제 종료",
        ["Dialog.ExternalProcess.StartNew"] = "새 프로세스 시작",

        ["Balloon.Attached"] = "{0}:{1} 서비스를 인계했습니다",
        ["Balloon.AttachFailed"] = "기존 프로세스를 인계할 수 없습니다.",

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

        ["Dialog.ExternalProcess.Title"] = "Processus externe détecté",
        ["Dialog.ExternalProcess.Message"] = "{0} est déjà en cours d'exécution sur {1}:{2}. Que souhaitez-vous faire ?",
        ["Dialog.ExternalProcess.Attach"] = "Reprendre le processus existant",
        ["Dialog.ExternalProcess.Kill"] = "Forcer l'arrêt de tous les processus",
        ["Dialog.ExternalProcess.StartNew"] = "Démarrer un nouveau processus",

        ["Balloon.Attached"] = "Service repris sur {0}:{1}",
        ["Balloon.AttachFailed"] = "Impossible de reprendre le processus existant.",

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

        ["Dialog.ExternalProcess.Title"] = "Externer Prozess erkannt",
        ["Dialog.ExternalProcess.Message"] = "{0} läuft bereits auf {1}:{2}. Was möchten Sie tun?",
        ["Dialog.ExternalProcess.Attach"] = "Vorhandenen Prozess übernehmen",
        ["Dialog.ExternalProcess.Kill"] = "Alle Prozesse zwangsbeenden",
        ["Dialog.ExternalProcess.StartNew"] = "Neuen Prozess starten",

        ["Balloon.Attached"] = "Dienst auf {0}:{1} übernommen",
        ["Balloon.AttachFailed"] = "Vorhandener Prozess konnte nicht übernommen werden.",

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

        ["Dialog.ExternalProcess.Title"] = "Proceso externo detectado",
        ["Dialog.ExternalProcess.Message"] = "{0} ya se está ejecutando en {1}:{2}. ¿Qué desea hacer?",
        ["Dialog.ExternalProcess.Attach"] = "Asumir el proceso existente",
        ["Dialog.ExternalProcess.Kill"] = "Forzar la detención de todos los procesos",
        ["Dialog.ExternalProcess.StartNew"] = "Iniciar un nuevo proceso",

        ["Balloon.Attached"] = "Servicio asumido en {0}:{1}",
        ["Balloon.AttachFailed"] = "No se pudo asumir el proceso existente.",

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

        ["Dialog.ExternalProcess.Title"] = "Rilevato processo esterno",
        ["Dialog.ExternalProcess.Message"] = "{0} è già in esecuzione su {1}:{2}. Cosa si desidera fare?",
        ["Dialog.ExternalProcess.Attach"] = "Subentrare al processo esistente",
        ["Dialog.ExternalProcess.Kill"] = "Terminare forzatamente tutti i processi",
        ["Dialog.ExternalProcess.StartNew"] = "Avviare un nuovo processo",

        ["Balloon.Attached"] = "Servizio subentrato su {0}:{1}",
        ["Balloon.AttachFailed"] = "Impossibile subentrare al processo esistente.",

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

        ["Dialog.ExternalProcess.Title"] = "Processo externo detectado",
        ["Dialog.ExternalProcess.Message"] = "{0} já está em execução em {1}:{2}. O que deseja fazer?",
        ["Dialog.ExternalProcess.Attach"] = "Assumir o processo existente",
        ["Dialog.ExternalProcess.Kill"] = "Forçar encerramento de todos os processos",
        ["Dialog.ExternalProcess.StartNew"] = "Iniciar um novo processo",

        ["Balloon.Attached"] = "Serviço assumido em {0}:{1}",
        ["Balloon.AttachFailed"] = "Não foi possível assumir o processo existente.",

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

        ["Dialog.ExternalProcess.Title"] = "Обнаружен внешний процесс",
        ["Dialog.ExternalProcess.Message"] = "{0} уже работает на {1}:{2}. Что вы хотите сделать?",
        ["Dialog.ExternalProcess.Attach"] = "Взять управление процессом",
        ["Dialog.ExternalProcess.Kill"] = "Принудительно завершить все процессы",
        ["Dialog.ExternalProcess.StartNew"] = "Запустить новый процесс",

        ["Balloon.Attached"] = "Управление службой на {0}:{1} принято",
        ["Balloon.AttachFailed"] = "Не удалось взять управление существующим процессом.",

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
