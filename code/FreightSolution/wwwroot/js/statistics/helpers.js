export function create(element) {
    return document.createElement(element);
}

export function empty(element) {
    while (element.firstChild)
        element.firstChild.remove();
}

export async function fetchData (url, body, controller) {
    const req = await fetch(url, {
        signal: controller.signal,
        method: 'POST',
        body: JSON.stringify(body),
        headers: {
            'content-type': 'application/json;charset=utf-8;'
        },
    });

    if (!req.ok)
        throw Error(`Something went wrong while fetching data from ${url}`);

    const json = await req.json();
    console.log(json);
    return json;
}

export function createButton(action, { text, icon, classList }) {
    const $button = $('<button>').addClass('btn btn-seconday ' + classList).click(action);
    if (icon)
        $button.append($(`<span class='${icon}'></span>`))

    if (text)
        $button.text(text);

    return $button.get(0);
}

export function formatDecimal(num,{ min = 0, max = 0, language = 'da-DK', autoFormat = false } = {}) {
    if (num === null || typeof num !== 'number' || isNaN(num)) num = 0;
    if (autoFormat) {
        if (num === 0) return '-';
        return num.toLocaleString(language, { maximumFractionDigits: Math.abs(num) < 1 ? 2 : (Math.abs(num) < 10 ? 1 : 0) });
    }
    
    return num.toLocaleString(language, { minimumFractionDigits: min, maximumFractionDigits: max });
}

export function compactNumber(n, lang = 'da-DK') {
    if (n === 0 || n === null) return '-';
    if (n < 1e2) return n.toLocaleString(lang, { maximumFractionDigits: 2 });
    if (n < 1e3) return n.toLocaleString(lang, { maximumFractionDigits: 0 });
    if (n < 1e6) return (n / 1e3).toLocaleString(lang, { maximumFractionDigits: 2 }) + 'K';
    if (n < 1e9) return (n / 1e6).toLocaleString(lang, { maximumFractionDigits: 2 }) + 'M';
    if (n < 1e12) return (n / 1e9).toLocaleString(long, { maximumFractionDigits: 2 }) + 'B';
    
    return null;
    // return Intl.NumberFormat('en-US', { notation: 'compact' ,maximumFractionDigits: 2 }).format(num);
}

export function formatString(str) {
    if (!str) return '';
    return str.trim()
        .split(/(?=[A-Z])/)
        .map(e => e.charAt(0).toUpperCase() + e.slice(1))
        .join(' ');
}

export function createExpandButton(action) {
    const $toggleButton = $('<span role="button" class="fa mr-1 plus-icon collapsed"></span>').click((e) => {
        e.currentTarget.classList.toggle('collapsed');
        action();
    });
    
    return $toggleButton.get(0);
}

export function getPercentColor(percent) {
    const colors = {
        0: { color: '#CBF1D1', text: '#000' },
        1: { color: '#97E2A3', text: '#000' },
        2: { color: '#85DD94', text: '#000' },
        3: { color: '#74D985', text: '#000' },
        4: { color: '#62D475', text: '#000' },
        5: { color: '#51CF66', text: '#000' },
        6: { color: '#41A652', text: '#000' },
        7: { color: '#399147', text: '#000' },
        8: { color: '#317C3D', text: '#fff' },
        9: { color: '#296833', text: '#fff' },
        10: { color: '#205329', text: '#fff' },
    }
    
    if (percent < 0 || percent === undefined || isNaN(percent)) return colors[0];
    if (percent > 100) return colors[10];
    
    percent = Math.floor(percent / 10);
    return colors[percent];
}

export function createIntervalString(interval, type) {
    if (type === 'lane' && interval.lane !== undefined) {
        return `${interval.lane.senderCountryISO ?? '?'} - ${interval.lane.receiverCountryISO ?? '?'}`;
    }

    if (type === 'interval') {
        return interval.endInterval === 0 ? '0 (empty)' : interval.endInterval === null
            ? (formatDecimal(interval.startInterval, { max: 1 }) + '+')
            : `${formatDecimal(interval.startInterval, { max: 1 })} - ${formatDecimal(interval.endInterval, { max: 1 })}`;
    }

    return null;
}

export function createToggleButton(text, action, isTriggered = false, id) {
    const toggleButton = document.querySelector('#toggle-button-template').content.cloneNode(true);

    $(toggleButton).find('.toggle-description').text(text);
    $(toggleButton).find('input').change((e) => {
        action(e.currentTarget);
    });

    if (typeof isTriggered === 'boolean')
        toggleButton.querySelector('input').checked = isTriggered;

    if (id)
        $(toggleButton).find('input').attr('id', id);

    return toggleButton;
}

export function createDelayedToggleButton({ title, action, id, delay = 200 } = {}) {
    let prevState = false;
    let timeout = null;

    const button = createToggleButton(title, (e) => {
        clearTimeout(timeout);
        const checked = e.checked;

        timeout = setTimeout(() => {
            if (checked === prevState) return;
            action(checked);
            prevState = checked;
        }, delay);
    }, false, id);
    
    const reset = () => {
        prevState = false;
        clearTimeout(timeout);
    }
    
    return button;
}

export function syncScrollbars(container1, container2) {
    let scrolling1 = false;
    let scrolling2 = false;
    let timeout = null;

    $(container1).on('scroll', ({ currentTarget: target }) => {
        if (!scrolling2) {
            clearTimeout(timeout);
            scrolling1 = true;
            container2.scrollLeft = target.scrollLeft;
            timeout = setTimeout(() => scrolling1 = false, 500);
        }
    });

    $(container2).on('scroll', ({ currentTarget: target }) => {
        if (!scrolling1) {
            clearTimeout(timeout);
            scrolling2 = true;
            container1.scrollLeft = target.scrollLeft;
            timeout = setTimeout(() => scrolling2 = false, 500);
        }
    });
}