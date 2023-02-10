import {
    compactNumber,
    createButton,
    formatDecimal,
    formatString,
    getPercentColor,
    createIntervalString, 
    createDelayedToggleButton,
    syncScrollbars
} from "../helpers.js";

export function buildMatrixByIntervalsAndLane(data) {
    const elements = [];
    const $container = $('<div>');
    const $header = $('<div>').addClass('mb-2 d-flex justify-content-between position-relative align-items-center');
    const $matrixContainer = $('<div>').addClass('matrix-container');

    // HEADER BUTTONS
    const $select = $('<select>', { 'class': "selectpicker" });
    for (const [key] of Object.entries(data.result.total)) {
        $select.append(`<option value='${key}'>${formatString(key)}</option>)`);
    }

    const [zoomButton, zoomModal] = createZoomButtonAndModal($container[0], $matrixContainer[0], postRenderExec);
    const percentToggleButton = createDelayedToggleButton({
        title: 'Percents',
        delay: 500,
        id: 'matrix-toggle-percents',
        action: (checked) => {
            toggleMatrixPercent(elements, checked, $select.val(), data.result);
        }
    });

    const $headerRightContainer = $('<div>').addClass('d-flex gap-1')
        .append([percentToggleButton, zoomButton, zoomModal])

    $header.append([$select, $headerRightContainer]);
    $select.selectpicker({
        style: 'btn select-with-transition',
    }).on('changed.bs.select', (e) => {
        const checked = $header.find('#matrix-toggle-percents').get(0).checked;
        toggleMatrixPercent(elements, checked, $select.val(), data.result);
    });
    // Default value
    $select.selectpicker('val', 'totalPrice');


    // Matrix Lanes
    const $tr = $('<tr>').append($('<th>'));
    for (const lane of data.result.lanes) {
        $tr.append($('<th>').text(lane.senderCountryISO + ' - ' + lane.receiverCountryISO));
    }

    const $headerTable = $('<table>').append($('<thead>').append($tr));
    const $headerContainer = $('<div>').addClass('header-container').append($headerTable);
    const $stickyTop = $('<div>').addClass('sticky').append($headerContainer);

    // Matrix Body
    const $tbody = $('<tbody>');
    for (const interval of data.result.intervals) {
        const intervalString = createIntervalString(interval, 'interval');
        const $row = $('<tr>').append($('<td>').text(intervalString));

        for (const lane of data.result.lanes) {
            const $td = $('<td>');
            const intervalData = interval.data[lane.laneString];
            if (intervalData !== undefined) {
                $td.text(compactNumber(intervalData[$select.val()])).addClass('clickable');
                elements.push({
                    ...intervalData,
                    element: $td.get(0),
                });

                $td.on('click', () => {
                    const val = $select.val();
                    $matrixContainer.prepend(createMatrixModal(lane, intervalString, intervalData, val));
                });
            }
            $row.append($td);
        }
        $tbody.append($row);
    }

    const $table = $('<table>').append($tbody);
    const $bodyContainer = $('<div>').addClass('body-container').append($table);

    // Sync scrolling on top and button scrollbar
    syncScrollbars($headerContainer[0], $bodyContainer[0])

    // Func to execute after the matrix has been rendered
    function postRenderExec() {
        const width = $table.find('tr:first-child td:first-child').outerWidth();
        $headerTable.find('tr th:first-child').css('min-width', width + 'px');
    }

    $container.append([
        $header,
        $matrixContainer.append([$stickyTop, $bodyContainer])
    ]);
    return [$container.get(0), postRenderExec];
}

function createMatrixModal(lane, interval, data, prop) {
    const $modal = $('<div>').addClass('matrix-modal');
    const $header = $('<div>')
        .addClass('card-header card-header-primary matrix-modal-header')
        .append($('<p>').text(`${lane.senderCountry} - ${lane.receiverCountry}`))
        .append($('<p>').text(interval));

    const $body = $('<div>').addClass('matrix-modal-body-container');
    for (const [key, value] of Object.entries(data)) {
        $body.append($('<div>').addClass(`matrix-modal-body-element ${prop === key ? 'text-bald' : ''}`)
            .css('font-size', '0.875rem')
            .append($('<p>').text(formatString(key)))
            .append($('<p>').text(formatDecimal(value, { autoFormat: true }))));
    }

    $modal.append($header).append($body).click(e => e.stopPropagation());
    const $backdrop = $('<div>').addClass('backdrop').append($modal).on('click', () => {
        $modal.remove();
        $backdrop.remove();
    });

    return $backdrop.get(0);
}

function toggleMatrixPercent(elements, toggle, target, data) {
    for (const e of elements) {
        let result = null;
        let textColor = null;
        let background = null;

        if (toggle) {
            const percent = e[target] / data.total[target] * 100;
            const relativePercent = ((e[target] - data.min[target]) / (data.max[target] - data.min[target])) * 100;
            const color = getPercentColor(relativePercent);

            textColor = color.text;
            background = color.color;
            result = formatDecimal(percent, { min: 2, max: 2 }) + '%';
        } else {
            result = compactNumber(e[target]);
        }

        e.element.innerText = result;
        e.element.style.background = background;
        e.element.style.color = textColor;
    }
}

function createZoomButtonAndModal(container, matrixContainer, action) {
    const DEFAULT = 100;
    let fontSize = DEFAULT;

    function updateMatrixZoom(resetButton) {
        resetButton.innerText = fontSize + '%';
        matrixContainer.style.fontSize = fontSize + '%';
        action();
    }

    // Buttons
    const zoomReset = createButton((e) => {
        fontSize = DEFAULT;
        updateMatrixZoom(e.currentTarget);
    }, { text: DEFAULT + '%', classList: 'reset-btn' });

    const zoomInButton = createButton(() => {
        if (fontSize >= 150) return;
        fontSize += 10;
        updateMatrixZoom(zoomReset);
    }, { icon: 'fas fa-plus' });

    const zoomOutButton = createButton(() => {
        if (fontSize <= 20) return;
        fontSize -= 10;
        updateMatrixZoom(zoomReset);
    }, { icon: 'fas fa-minus' })

    // Modal
    const $zoomModal = $('<div>').addClass('modal-fields')
        .append([zoomOutButton, zoomReset, zoomInButton])
        .click(e => e.stopPropagation())
        .hide();

    const $zoomToggleButton = $('<button>').addClass('btn btn-primary').text('Zoom').click(e => {
        e.stopPropagation();
        $zoomModal.fadeToggle(100);
        $(e.currentTarget).toggleClass('pressed');
    });

    $(container).click(() => {
        $zoomModal.fadeOut();
        $zoomToggleButton.removeClass('pressed');
    });

    return [$zoomToggleButton.get(0), $zoomModal.get(0)];
}