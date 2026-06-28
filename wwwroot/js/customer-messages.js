'use strict';

let conversationPaneAbortController = null;
let conversationRequestAbortController = null;

document.addEventListener('DOMContentLoaded', () => {
    bindToastDismiss();
    bindRealtime();
});

function bindToastDismiss() {
    document.querySelectorAll('[data-dismiss-target]').forEach(button => {
        button.addEventListener('click', () => {
            document.getElementById(button.dataset.dismissTarget)?.remove();
        });
    });

    ['toastSuccess', 'toastError'].forEach(id => {
        const element = document.getElementById(id);
        if (element) {
            setTimeout(() => element.remove(), 5000);
        }
    });
}

function bindReplyComposer(signal) {
    const form = document.querySelector('[data-message-reply-form]');
    const textarea = form?.querySelector('textarea');
    if (!form || !textarea) {
        return;
    }

    const resize = () => {
        textarea.style.height = 'auto';
        textarea.style.height = `${Math.min(Math.max(textarea.scrollHeight, 42), 120)}px`;
    };

    textarea.addEventListener('input', resize, { signal });
    textarea.addEventListener('keydown', event => {
        if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
            event.preventDefault();
            form.requestSubmit();
        }
    }, { signal });
    resize();
}

function bindDetailsPanel(signal) {
    const root = document.querySelector('.messenger-page');
    const panel = root?.querySelector('[data-customer-details-panel]');
    if (!root || !panel) {
        return;
    }

    const desktopQuery = window.matchMedia('(min-width: 1400px)');
    const toggles = root.querySelectorAll('[data-customer-details-toggle]');
    const closeButtons = root.querySelectorAll('[data-customer-details-close]');

    const sync = () => {
        const expanded = desktopQuery.matches
            ? !root.classList.contains('is-details-collapsed')
            : root.classList.contains('is-details-open');

        toggles.forEach(button => button.setAttribute('aria-expanded', String(expanded)));
        panel.setAttribute('aria-hidden', String(!expanded));
    };

    const toggle = () => {
        if (desktopQuery.matches) {
            root.classList.toggle('is-details-collapsed');
        } else {
            root.classList.toggle('is-details-open');
        }
        sync();
    };

    const close = () => {
        if (desktopQuery.matches) {
            root.classList.add('is-details-collapsed');
        } else {
            root.classList.remove('is-details-open');
        }
        sync();
    };

    toggles.forEach(button => button.addEventListener('click', toggle, { signal }));
    closeButtons.forEach(button => button.addEventListener('click', close, { signal }));
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            close();
        }
    }, { signal });
    desktopQuery.addEventListener('change', () => {
        root.classList.remove('is-details-open', 'is-details-collapsed');
        sync();
    }, { signal });
    sync();
}

function bindOlderMessages(signal) {
    const button = document.querySelector('[data-load-older-messages]');
    const list = document.querySelector('[data-message-list]');
    if (!button || !list) {
        return;
    }

    button.addEventListener('click', async () => {
        const beforeId = Number(button.dataset.beforeId || 0);
        if (!beforeId || button.disabled) {
            return;
        }

        button.disabled = true;
        const previousHeight = list.scrollHeight;
        try {
            const url = new URL(button.dataset.url, window.location.origin);
            url.searchParams.set('beforeId', String(beforeId));
            url.searchParams.set('take', '50');
            const response = await fetch(url, {
                credentials: 'same-origin',
                headers: { Accept: 'application/json' },
            });
            const page = await response.json().catch(() => null);
            if (!response.ok || !page) {
                throw new Error('Không thể tải tin nhắn cũ hơn.');
            }

            const firstExisting = list.querySelector('.customer-message-bubble');
            (page.messages || []).forEach(message => {
                if (document.querySelector(`[data-message-id="${message.id}"]`)) {
                    return;
                }

                list.insertBefore(createMessageElement(message), firstExisting);
            });
            window.lucide?.createIcons();
            list.scrollTop += list.scrollHeight - previousHeight;

            if (!page.hasMore || !page.nextBeforeId) {
                button.remove();
            } else {
                button.dataset.beforeId = page.nextBeforeId;
            }
        } catch (error) {
            showRealtimeNotice(error.message || 'Không thể tải tin nhắn cũ hơn.');
        } finally {
            button.disabled = false;
        }
    }, { signal });
}

function bindRealtime() {
    const root = document.querySelector('[data-customer-message-realtime="workspace"]');
    if (!root) {
        return;
    }

    if (!window.signalR) {
        bindConversationPane(root, null);
        bindConversationNavigation(root, null);
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/customer-messages')
        .withAutomaticReconnect()
        .build();

    connection.on('MessageReceived', payload => handleMessageReceived(root, connection, payload));
    connection.on('ConversationChanged', payload => handleConversationChanged(root, payload));
    connection.on('ConversationStatusChanged', payload => handleConversationStatusChanged(root, payload));

    bindConversationPane(root, connection);
    bindConversationNavigation(root, connection);
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') {
            markCurrentConversationRead(root, connection);
        }
    });

    const joinCurrentConversation = async () => {
        const conversationId = Number(root.dataset.conversationId || 0);
        if (conversationId > 0) {
            await connection.invoke('JoinConversation', conversationId);
        }
    };

    connection.onreconnected(() => joinCurrentConversation().catch(() => {
        showRealtimeNotice('Đã kết nối lại nhưng chưa thể mở lại hội thoại realtime.');
    }));

    connection.start()
        .then(joinCurrentConversation)
        .catch(() => {
            showRealtimeNotice('Không thể kết nối realtime. Tin nhắn vẫn có thể gửi theo cách thông thường.');
        });
}

function bindConversationPane(root, connection) {
    conversationPaneAbortController?.abort();
    conversationPaneAbortController = new AbortController();
    const { signal } = conversationPaneAbortController;

    bindReplyComposer(signal);
    bindDetailsPanel(signal);
    bindOlderMessages(signal);
    bindRealtimeReplyForm(root, connection, signal);
    scrollThreadToBottom(false);
}

function bindRealtimeReplyForm(root, connection, signal) {
    const form = document.querySelector('[data-message-reply-form]');
    if (!form || !connection) {
        return;
    }

    form.addEventListener('submit', async event => {
        if (!isSignalRConnected(connection)) {
            return;
        }

        event.preventDefault();
        const textarea = form.querySelector('textarea[name="Body"]');
        const clientMessageInput = form.querySelector('input[name="ClientMessageId"]');
        const body = textarea?.value.trim() || '';
        const conversationId = Number(root.dataset.conversationId || 0);
        const clientMessageId = clientMessageInput?.value || createClientMessageId();

        if (!body || conversationId <= 0) {
            textarea?.focus();
            return;
        }

        const button = form.querySelector('button[type="submit"]');
        setFormBusy(textarea, button, true);

        try {
            const result = await connection.invoke('SendStaffReply', {
                conversationId,
                clientMessageId,
                body,
            });

            if (!result?.succeeded) {
                showRealtimeNotice(result?.message || 'Không thể gửi phản hồi realtime.');
                return;
            }

            textarea.value = '';
            if (clientMessageInput) {
                clientMessageInput.value = createClientMessageId();
            }
            textarea.dispatchEvent(new Event('input', { bubbles: true }));
            textarea.focus();
        } catch {
            showRealtimeNotice('Kết nối realtime bị gián đoạn. Vui lòng thử lại.');
        } finally {
            setFormBusy(textarea, button, false);
        }
    }, { signal });
}

function bindConversationNavigation(root, connection) {
    if (root.dataset.conversationNavigationBound === 'true') {
        return;
    }

    root.dataset.conversationNavigationBound = 'true';
    history.replaceState({ ...history.state, customerMessageWorkspace: true }, '', window.location.href);

    root.addEventListener('click', event => {
        const mobileBack = event.target.closest('.messenger-mobile-back');
        if (mobileBack && event.button === 0 && !event.metaKey && !event.ctrlKey && !event.shiftKey) {
            event.preventDefault();
            root.classList.add('is-list-only');
            return;
        }

        const link = event.target.closest('.messenger-conversation-item');
        if (!link || !shouldHandleConversationClick(event, link)) {
            return;
        }

        event.preventDefault();
        loadConversation(root, connection, link.href, link, true);
    });

    window.addEventListener('popstate', () => {
        loadConversation(root, connection, window.location.href, null, false);
    });
}

function shouldHandleConversationClick(event, link) {
    return event.button === 0 &&
        !event.defaultPrevented &&
        !event.metaKey &&
        !event.ctrlKey &&
        !event.shiftKey &&
        !event.altKey &&
        link.target !== '_blank' &&
        new URL(link.href, window.location.href).origin === window.location.origin;
}

async function loadConversation(root, connection, url, sourceRow, updateHistory) {
    conversationRequestAbortController?.abort();
    const requestController = new AbortController();
    conversationRequestAbortController = requestController;

    const previousConversationId = Number(root.dataset.conversationId || 0);
    root.classList.add('is-switching');
    root.setAttribute('aria-busy', 'true');
    sourceRow?.classList.add('is-loading');

    try {
        const response = await fetch(url, {
            credentials: 'same-origin',
            headers: {
                Accept: 'text/html',
                'X-Requested-With': 'XMLHttpRequest',
            },
            signal: requestController.signal,
        });
        if (!response.ok) {
            throw new Error('Không thể mở hội thoại này.');
        }

        const documentText = await response.text();
        const nextDocument = new DOMParser().parseFromString(documentText, 'text/html');
        const nextRoot = nextDocument.querySelector('[data-customer-message-realtime="workspace"]');
        const nextChat = nextRoot?.querySelector(':scope > .messenger-chat');
        const currentChat = root.querySelector(':scope > .messenger-chat');
        if (!nextRoot || !nextChat || !currentChat) {
            throw new Error('Dữ liệu hội thoại không hợp lệ.');
        }

        const adoptedChat = nextChat.cloneNode(true);
        currentChat.replaceWith(adoptedChat);
        root.querySelector(':scope > .messenger-details-backdrop')?.remove();
        root.querySelector(':scope > .messenger-details')?.remove();

        let insertionPoint = adoptedChat;
        [
            nextRoot.querySelector(':scope > .messenger-details-backdrop'),
            nextRoot.querySelector(':scope > .messenger-details'),
        ].forEach(element => {
            if (!element) return;
            const clone = element.cloneNode(true);
            insertionPoint.after(clone);
            insertionPoint = clone;
        });

        syncWorkspaceState(root, nextRoot);
        const conversationId = Number(root.dataset.conversationId || 0);
        syncSelectedConversationRow(root, nextRoot, conversationId);
        bindConversationPane(root, connection);
        window.lucide?.createIcons();

        if (updateHistory) {
            history.pushState(
                { customerMessageWorkspace: true, conversationId },
                '',
                response.url || url);
        }

        await switchRealtimeConversation(connection, previousConversationId, conversationId);
        await syncLatestConversationMessages(conversationId);
    } catch (error) {
        if (error.name !== 'AbortError') {
            showRealtimeNotice(error.message || 'Không thể mở hội thoại này.');
        }
    } finally {
        sourceRow?.classList.remove('is-loading');
        if (conversationRequestAbortController === requestController) {
            conversationRequestAbortController = null;
            root.classList.remove('is-switching');
            root.removeAttribute('aria-busy');
        }
    }
}

function syncWorkspaceState(root, nextRoot) {
    root.dataset.conversationId = nextRoot.dataset.conversationId || '0';
    root.dataset.filterUnread = nextRoot.dataset.filterUnread || 'false';
    root.dataset.filterSearch = nextRoot.dataset.filterSearch || '';
    root.dataset.filterStatus = nextRoot.dataset.filterStatus || '';
    root.dataset.filterAi = nextRoot.dataset.filterAi || '';
    root.dataset.page = nextRoot.dataset.page || '1';
    root.dataset.pageSize = nextRoot.dataset.pageSize || '20';
    root.classList.toggle('has-selection', nextRoot.classList.contains('has-selection'));
    root.classList.toggle('is-list-only', nextRoot.classList.contains('is-list-only'));
    root.classList.remove('is-details-open', 'is-details-collapsed');
}

function syncSelectedConversationRow(root, nextRoot, conversationId) {
    root.querySelectorAll('[data-conversation-row]').forEach(row => {
        row.classList.remove('is-selected');
        row.removeAttribute('aria-current');
    });

    const row = root.querySelector(`[data-conversation-row="${conversationId}"]`);
    if (row) {
        row.classList.add('is-selected');
        row.setAttribute('aria-current', 'true');
        setUnreadPill(row, 0);
        if (root.dataset.filterUnread === 'true') {
            row.remove();
        }
    }

    const nextTotal = nextRoot.querySelector('.messenger-total')?.textContent?.trim();
    setText(root.querySelector('.messenger-total'), nextTotal || '0');
    const nextUnread = nextRoot.querySelector('[data-unread-filter-count]')?.textContent?.trim();
    updateUnreadFilterCounts(Number(nextUnread || 0));
}

async function switchRealtimeConversation(connection, previousConversationId, conversationId) {
    if (!isSignalRConnected(connection)) {
        return;
    }

    if (previousConversationId > 0 && previousConversationId !== conversationId) {
        await connection.invoke('LeaveConversation', previousConversationId).catch(() => undefined);
    }

    if (conversationId > 0) {
        try {
            const result = await connection.invoke('JoinConversation', conversationId);
            if (!result?.succeeded) {
                showRealtimeNotice(result?.message || 'Không thể mở hội thoại realtime.');
            }
        } catch {
            showRealtimeNotice('Hội thoại đã mở nhưng kết nối realtime bị gián đoạn.');
        }
    }
}

async function syncLatestConversationMessages(conversationId) {
    if (conversationId <= 0) return;

    try {
        const url = new URL('/CustomerMessages/Messages', window.location.origin);
        url.searchParams.set('id', String(conversationId));
        url.searchParams.set('take', '50');
        const response = await fetch(url, {
            credentials: 'same-origin',
            headers: { Accept: 'application/json' },
        });
        const page = await response.json().catch(() => null);
        if (!response.ok || !page) return;

        let appended = false;
        (page.messages || []).forEach(message => {
            if (document.querySelector(`[data-message-id="${message.id}"]`)) return;
            appendMessage(message, false);
            appended = true;
        });
        if (appended) {
            window.lucide?.createIcons();
            scrollThreadToBottom(false);
        }
    } catch {
        // The rendered pane remains usable; realtime will continue delivering new messages.
    }
}

function isSignalRConnected(connection) {
    return connection &&
        window.signalR &&
        connection.state === signalR.HubConnectionState.Connected;
}

function createClientMessageId() {
    if (window.crypto?.randomUUID) {
        return window.crypto.randomUUID();
    }

    return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 12)}`;
}

function setFormBusy(textarea, button, isBusy) {
    if (textarea) {
        textarea.disabled = isBusy;
    }

    if (button) {
        button.disabled = isBusy;
        button.classList.toggle('is-busy', isBusy);
    }
}

async function handleMessageReceived(root, connection, payload) {
    if (!payload) {
        return;
    }

    const currentConversationId = Number(root.dataset.conversationId || 0);
    const incomingConversationId = Number(payload.conversationId || 0);

    if (incomingConversationId === currentConversationId) {
        appendMessage(payload);
        updateDetailsFromConversation(payload.conversation);
    }

    if (payload.conversation) {
        updateIndexRow(root, payload.conversation);
    }

    if (incomingConversationId === currentConversationId &&
        payload.sender === 'Customer' &&
        document.visibilityState === 'visible') {
        await markCurrentConversationRead(root, connection);
    }
}

function handleConversationChanged(root, payload) {
    if (!payload) {
        return;
    }

    updateIndexRow(root, payload);

    const currentConversationId = Number(root.dataset.conversationId || 0);
    if (Number(payload.id || 0) === currentConversationId) {
        updateDetailsFromConversation(payload);
    }
}

function handleConversationStatusChanged(root, payload) {
    if (!payload) {
        return;
    }

    updateIndexRow(root, payload);

    const currentConversationId = Number(root.dataset.conversationId || 0);
    if (Number(payload.id || 0) === currentConversationId) {
        updateDetailsFromConversation(payload);
    }
}

async function markCurrentConversationRead(root, connection) {
    if (!isSignalRConnected(connection) || root.dataset.markingRead === 'true') {
        return;
    }

    const conversationId = Number(root.dataset.conversationId || 0);
    const row = document.querySelector(`[data-conversation-row="${conversationId}"]`);
    if (conversationId <= 0 || !row?.querySelector('[data-row-unread]')) {
        return;
    }

    root.dataset.markingRead = 'true';
    try {
        const result = await connection.invoke('MarkConversationRead', conversationId);
        if (!result?.succeeded) {
            showRealtimeNotice(result?.message || 'Không thể đánh dấu hội thoại đã đọc.');
        }
    } catch {
        showRealtimeNotice('Không thể đồng bộ trạng thái đã đọc realtime.');
    } finally {
        delete root.dataset.markingRead;
    }
}

function appendMessage(payload, shouldScroll = true) {
    const list = document.querySelector('[data-message-list]');
    if (!list || !payload || document.querySelector(`[data-message-id="${payload.id}"]`)) {
        return;
    }

    document.querySelector('[data-message-empty]')?.remove();

    const article = createMessageElement(payload);
    list.append(article);
    if (shouldScroll) {
        window.lucide?.createIcons();
        scrollThreadToBottom(true);
    }
}

function createMessageElement(payload) {
    const article = document.createElement('article');
    article.className = `customer-message-bubble ${payload.senderClass || ''}`.trim();
    article.dataset.messageId = payload.id;
    article.id = `message-${payload.id}`;

    if (payload.sender !== 'Staff') {
        const avatar = document.createElement('span');
        avatar.className = 'messenger-message-avatar';
        avatar.setAttribute('aria-hidden', 'true');

        if (payload.sender === 'Ai') {
            const icon = document.createElement('i');
            icon.dataset.lucide = 'bot';
            avatar.append(icon);
        } else {
            avatar.textContent = getInitials(payload.senderName || 'Khách hàng');
        }

        article.append(avatar);
    }

    const stack = document.createElement('div');
    stack.className = 'messenger-message-stack';

    const head = document.createElement('div');
    head.className = 'customer-message-bubble-head';

    const senderName = document.createElement('strong');
    senderName.textContent = payload.senderName || payload.senderLabel || '';
    head.append(senderName);

    if (payload.sender === 'Ai') {
        const sender = document.createElement('span');
        sender.className = 'customer-message-sender is-ai';
        sender.textContent = 'AI';
        head.append(sender);
    }

    const time = document.createElement('time');
    time.dateTime = payload.createdAtIso || '';
    time.textContent = payload.createdAtText || '';
    head.append(time);

    const messageBody = document.createElement('div');
    messageBody.className = 'messenger-message-body';

    const body = document.createElement('p');
    body.textContent = payload.body || '';
    messageBody.append(body);

    if (payload.sender === 'Ai') {
        appendAiMeta(messageBody, payload);
    }

    stack.append(head, messageBody);
    article.append(stack);
    return article;
}

function getInitials(name) {
    return String(name || 'K')
        .trim()
        .split(/\s+/)
        .slice(0, 2)
        .map(part => part.charAt(0).toUpperCase())
        .join('');
}

function appendAiMeta(parent, payload) {
    const hasMeta = payload.aiProvider ||
        payload.aiModel ||
        payload.aiResponseId ||
        payload.aiPrompt ||
        payload.aiMetadataJson;

    if (!hasMeta) {
        return;
    }

    const meta = document.createElement('div');
    meta.className = 'customer-message-ai-meta';

    appendMetaChip(meta, 'Provider', payload.aiProvider);
    appendMetaChip(meta, 'Model', payload.aiModel);
    appendMetaChip(meta, 'Response', payload.aiResponseId);
    appendMetaDetails(meta, 'Prompt', payload.aiPrompt);
    appendMetaDetails(meta, 'Metadata', payload.aiMetadataJson);

    parent.append(meta);
}

function appendMetaChip(parent, label, value) {
    if (!value) {
        return;
    }

    const chip = document.createElement('span');
    chip.textContent = `${label}: ${value}`;
    parent.append(chip);
}

function appendMetaDetails(parent, label, value) {
    if (!value) {
        return;
    }

    const details = document.createElement('details');
    const summary = document.createElement('summary');
    const pre = document.createElement('pre');
    summary.textContent = label;
    pre.textContent = value;
    details.append(summary, pre);
    parent.append(details);
}

function scrollThreadToBottom(smooth) {
    const thread = document.querySelector('[data-message-list]');
    if (!thread) {
        return;
    }

    requestAnimationFrame(() => {
        thread.scrollTo({
            top: thread.scrollHeight,
            behavior: smooth ? 'smooth' : 'auto',
        });
    });
}

function updateDetailsFromConversation(conversation) {
    if (!conversation) {
        return;
    }

    setMetric('messageCount', conversation.messageCount);
    setMetric('aiCount', conversation.aiMessageCount);
    setMetric('lastCustomerMessageAt', conversation.lastCustomerMessageAtText || 'Chưa có');
    setMetric('lastStaffMessageAt', conversation.lastStaffMessageAtText || 'Chưa có');
    setMetric('lastAiMessageAt', conversation.lastAiMessageAtText || 'Chưa có');

    document.querySelectorAll('[data-conversation-status]').forEach(element => {
        setStatusPill(element, conversation.statusClass, conversation.statusLabel);
    });
}

function setMetric(name, value) {
    document.querySelectorAll(`[data-message-metric="${name}"]`).forEach(element => {
        element.textContent = value ?? '0';
    });
}

function updateIndexRow(root, conversation) {
    updateUnreadFilterCounts(conversation.totalUnreadCustomerMessageCount);

    let row = document.querySelector(`[data-conversation-row="${conversation.id}"]`);
    if (!row) {
        if (!conversationMatchesCurrentList(root, conversation)) {
            return;
        }

        const list = root.querySelector('.messenger-conversation-scroll');
        if (!list) return;

        list.querySelector('.messenger-list-empty')?.remove();
        row = createConversationRow(root, conversation);
        list.prepend(row);
        if (isInitialConversationPayload(conversation)) {
            incrementConversationTotal(root);
        }
        trimConversationList(root, list);
        window.lucide?.createIcons();
    }

    row.classList.add('is-live-updated');
    setTimeout(() => row.classList.remove('is-live-updated'), 2400);

    setText(row.querySelector('[data-row-preview]'), conversation.lastMessagePreview || 'Chưa có nội dung.');
    setText(row.querySelector('[data-row-sender]'), `${conversation.lastMessageSenderLabel || 'Tin nhắn'}:`);
    setText(row.querySelector('[data-row-time]'), conversation.lastMessageAtText || 'Vừa xong');
    setStatusPill(row.querySelector('[data-row-status]'), conversation.statusClass, conversation.statusLabel);
    setAiCount(row, conversation.aiMessageCount);
    setUnreadPill(row, conversation.unreadCustomerMessageCount);

    if (root.dataset.filterUnread === 'true' && Number(conversation.unreadCustomerMessageCount || 0) <= 0) {
        row.remove();
        return;
    }

    const list = row.closest('.messenger-conversation-scroll');
    if (list && list.firstElementChild !== row) {
        list.prepend(row);
    }
}

function conversationMatchesCurrentList(root, conversation) {
    if (Number(root.dataset.page || 1) !== 1) {
        return false;
    }

    if (root.dataset.filterUnread === 'true' &&
        Number(conversation.unreadCustomerMessageCount || 0) <= 0) {
        return false;
    }

    const status = (root.dataset.filterStatus || '').trim().toLowerCase();
    if (status && String(conversation.status || '').toLowerCase() !== status) {
        return false;
    }

    const aiFilter = (root.dataset.filterAi || '').trim().toLowerCase();
    if (aiFilter === 'with-ai' && Number(conversation.aiMessageCount || 0) <= 0) {
        return false;
    }

    const search = normalizeSearchText(root.dataset.filterSearch || '');
    if (!search) {
        return true;
    }

    const searchableText = normalizeSearchText([
        conversation.customerName,
        conversation.customerEmail,
        conversation.customerPhone,
        conversation.subject,
        conversation.lastMessagePreview,
    ].filter(Boolean).join(' '));
    return searchableText.includes(search);
}

function normalizeSearchText(value) {
    return String(value || '').trim().toLocaleLowerCase('vi-VN');
}

function createConversationRow(root, conversation) {
    const row = document.createElement('a');
    row.href = buildConversationUrl(conversation.id);
    row.className = 'messenger-conversation-item';
    row.dataset.conversationRow = String(conversation.id);

    const isSelected = Number(root.dataset.conversationId || 0) === Number(conversation.id);
    row.classList.toggle('is-selected', isSelected);
    if (isSelected) {
        row.setAttribute('aria-current', 'true');
    }

    const avatar = document.createElement('span');
    avatar.className = 'messenger-avatar';
    avatar.setAttribute('aria-hidden', 'true');
    avatar.textContent = getInitials(conversation.customerName || 'Khách hàng');
    if (String(conversation.status || '').toLowerCase() === 'open') {
        const presence = document.createElement('span');
        presence.className = 'messenger-presence';
        presence.title = 'Hội thoại đang mở';
        avatar.append(presence);
    }

    const copy = document.createElement('span');
    copy.className = 'messenger-conversation-copy';
    const line = document.createElement('span');
    line.className = 'messenger-conversation-line';
    const name = document.createElement('strong');
    name.textContent = conversation.customerName || 'Khách hàng';
    const time = document.createElement('time');
    time.dataset.rowTime = '';
    time.dateTime = conversation.lastMessageAtIso || '';
    time.textContent = conversation.lastMessageAtText || 'Vừa xong';
    line.append(name, time);

    const preview = document.createElement('span');
    preview.className = 'messenger-conversation-preview';
    const sender = document.createElement('span');
    sender.dataset.rowSender = '';
    sender.textContent = `${conversation.lastMessageSenderLabel || 'Tin nhắn'}:`;
    const body = document.createElement('span');
    body.dataset.rowPreview = '';
    body.textContent = conversation.lastMessagePreview || 'Chưa có nội dung.';
    preview.append(sender, body);

    const meta = document.createElement('span');
    meta.className = 'messenger-conversation-meta';
    const status = document.createElement('span');
    status.dataset.rowStatus = '';
    setStatusPill(status, conversation.statusClass, conversation.statusLabel);
    meta.append(status);
    copy.append(line, preview, meta);
    row.append(avatar, copy);

    setAiCount(row, conversation.aiMessageCount);
    setUnreadPill(row, conversation.unreadCustomerMessageCount);
    return row;
}

function buildConversationUrl(conversationId) {
    const url = new URL(window.location.href);
    url.pathname = '/CustomerMessages';
    url.searchParams.set('id', String(conversationId));
    url.searchParams.delete('listOnly');
    return url.toString();
}

function incrementConversationTotal(root) {
    const total = root.querySelector('.messenger-total');
    if (!total) return;
    total.textContent = String(Number(total.textContent || 0) + 1);
}

function isInitialConversationPayload(conversation) {
    const channel = String(conversation.channel || '').toLowerCase();
    const messageCount = Number(conversation.messageCount || 0);
    return (channel === 'support' && messageCount === 1) ||
        (channel === 'ai' && messageCount === 2);
}

function trimConversationList(root, list) {
    const pageSize = Number(root.dataset.pageSize || 20);
    const rows = Array.from(list.querySelectorAll('[data-conversation-row]'));
    if (pageSize <= 0 || rows.length <= pageSize) return;

    const removable = rows.reverse().find(row => !row.classList.contains('is-selected'));
    removable?.remove();
}

function updateUnreadFilterCounts(count) {
    const value = Number(count || 0);
    document.querySelectorAll('[data-unread-filter-count]').forEach(element => {
        element.textContent = String(value);
        element.hidden = value <= 0;
    });
}

function setText(element, value) {
    if (element) {
        element.textContent = value;
    }
}

function setStatusPill(element, statusClass, label) {
    if (!element) {
        return;
    }

    element.className = `customer-message-status ${statusClass || ''}`.trim();

    let dot = element.querySelector('.dot');
    if (!dot) {
        dot = document.createElement('span');
        dot.className = 'dot';
        element.prepend(dot);
    }

    let labelElement = Array.from(element.children).find(child => !child.classList.contains('dot'));
    if (!labelElement) {
        labelElement = document.createElement('span');
        element.append(labelElement);
    }

    labelElement.textContent = label || '';
}

function setAiCount(row, count) {
    const value = Number(count || 0);
    let element = row.querySelector('[data-row-ai]');

    if (value <= 0) {
        element?.remove();
        return;
    }

    if (!element) {
        element = document.createElement('span');
        element.className = 'messenger-ai-count';
        element.dataset.rowAi = '';
        element.title = 'Tin nhắn AI';
        const icon = document.createElement('i');
        icon.dataset.lucide = 'bot';
        element.append(icon);
        row.querySelector('.messenger-conversation-meta')?.append(element);
    }

    Array.from(element.childNodes)
        .filter(node => node.nodeType === Node.TEXT_NODE)
        .forEach(node => node.remove());
    element.append(document.createTextNode(String(value)));
    window.lucide?.createIcons();
}

function setUnreadPill(row, count) {
    const value = Number(count || 0);
    let pill = row.querySelector('[data-row-unread]');

    row.classList.toggle('is-unread', value > 0);
    if (value <= 0) {
        pill?.remove();
        return;
    }

    if (!pill) {
        pill = document.createElement('span');
        pill.className = 'customer-message-unread';
        pill.dataset.rowUnread = '';
        row.append(pill);
    }

    pill.textContent = String(value);
    pill.setAttribute('aria-label', `${value} tin chưa đọc`);
}

function showRealtimeNotice(message) {
    const notice = document.querySelector('[data-realtime-notice]');
    if (!notice) {
        return;
    }

    setText(notice.querySelector('[data-realtime-notice-text]'), message);
    notice.hidden = false;

    const reload = notice.querySelector('[data-realtime-reload]');
    if (reload) {
        reload.onclick = () => window.location.reload();
    }
}
