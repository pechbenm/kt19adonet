// Клиент чата: подключение к защищённому хабу SignalR (/hubs/chat).
// Браузер сам отправляет auth-куку; без неё сервер отвечает 401.
(() => {
    const root = document.getElementById("chat");
    const me = root.dataset.user;

    const statusEl = document.getElementById("status");
    const listEl = document.getElementById("messages");
    const usersEl = document.getElementById("users");
    const countEl = document.getElementById("users-count");
    const errorEl = document.getElementById("error");
    const form = document.getElementById("send-form");
    const input = document.getElementById("message-input");
    const button = document.getElementById("send-btn");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    function setStatus(text, online) {
        statusEl.textContent = text;
        statusEl.classList.toggle("is-online", online);
        input.disabled = !online;
        button.disabled = !online;
        if (online) input.focus();
    }

    function formatTime(iso) {
        return new Date(iso).toLocaleTimeString("ru-RU", { hour: "2-digit", minute: "2-digit" });
    }

    // Всё выводим через textContent — сообщения других пользователей не могут внедрить HTML/JS (XSS).
    function addMessage(msg) {
        const stick = listEl.scrollHeight - listEl.scrollTop - listEl.clientHeight < 60;

        const li = document.createElement("li");
        li.className = "msg" + (msg.user === me ? " msg--mine" : "");

        const meta = document.createElement("div");
        meta.className = "msg__meta";
        const author = document.createElement("strong");
        author.textContent = msg.user;
        const time = document.createElement("span");
        time.textContent = formatTime(msg.sentAt);
        meta.append(author, time);

        const text = document.createElement("div");
        text.className = "msg__text";
        text.textContent = msg.text;

        li.append(meta, text);
        listEl.append(li);
        if (stick) listEl.scrollTop = listEl.scrollHeight;
    }

    function renderUsers(names) {
        usersEl.replaceChildren(...names.map(n => {
            const li = document.createElement("li");
            li.textContent = n === me ? n + " (вы)" : n;
            return li;
        }));
        countEl.textContent = names.length;
    }

    connection.on("LoadHistory", messages => {
        listEl.replaceChildren();
        messages.forEach(addMessage);
        listEl.scrollTop = listEl.scrollHeight;
    });
    connection.on("ReceiveMessage", addMessage);
    connection.on("UsersChanged", renderUsers);

    connection.onreconnecting(() => setStatus("Соединение потеряно, переподключение…", false));
    connection.onreconnected(() => setStatus("Подключено", true));
    connection.onclose(() => setStatus("Отключено. Обновите страницу или войдите заново.", false));

    form.addEventListener("submit", async e => {
        e.preventDefault();
        const text = input.value.trim();
        if (!text) return;

        errorEl.textContent = "";
        try {
            await connection.invoke("SendMessage", text);
            input.value = "";
        } catch (err) {
            // HubException с сервера приходит в тексте ошибки.
            errorEl.textContent = String(err.message || err).replace(/^.*HubException:\s*/, "");
        }
        input.focus();
    });

    async function start() {
        try {
            await connection.start();
            setStatus("Подключено", true);
        } catch (err) {
            const unauthorized = err && (err.statusCode === 401 || /401|Unauthorized/i.test(String(err)));
            if (unauthorized) {
                location.href = "/Account/Login?ReturnUrl=%2FChat";   // сессия истекла
                return;
            }
            setStatus("Не удалось подключиться. Повтор через 5 секунд…", false);
            setTimeout(start, 5000);
        }
    }

    start();
})();
