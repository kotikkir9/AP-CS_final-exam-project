import {
    create,
    createExpandButton,
    createToggleButton,
    empty,
    formatDecimal,
    createIntervalString,
    createDelayedToggleButton
} from "../helpers.js";
import {createFieldToggleButtons, createRow, createTableWithHeader} from "../components/tableBuilder.js";
import {buildMatrixByIntervalsAndLane} from "../components/matrix.js";

const layout = [
    { description: 'Overall', value: 0 },
    { description: 'By Carrier', value: 2 },
    { description: 'Matrix By Lane', value: 3 },
];

export const kgIntervals = {
    endPoint: "WeightIntervals",
    build: build,
    layout: layout,
    updateFilters: (container) => updateFilters(container),
    setupFilters: (container) => setDefaultIntervalFilters(container, 100, [1, 5, 10, 50, 100, 1000]),
};

export const qtyIntervals = {
    endPoint: "QuantityIntervals",
    build: build,
    layout: layout,
    updateFilters: (container) => updateFilters(container),
    setupFilters: (container) => setDefaultIntervalFilters(container, 35, [1, 5, 10, 50])
}

export const cbmIntervals = {
    endPoint: "CubicMeterIntervals",
    build: build,
    layout: layout,
    updateFilters: (container) => updateFilters(container),
    setupFilters: (container) => setDefaultIntervalFilters(container, 50, [1, 5, 10, 50, 100], 5)
}

export const ldmIntervals = {
    endPoint: "LoadMeterIntervals",
    build: build,
    layout: layout,
    updateFilters: (container) => updateFilters(container),
    setupFilters: (container) => setDefaultIntervalFilters(container, 13.6, [0.1, 0.2, 0.4, 1, 2, 5, 10], 1),
}

export const laneIntervals = {
    endPoint: "LaneIntervals",
    build: (data) => build(data, 'lane'),
    layout: [
        { description: 'Overall', value: 0 },
        { description: 'By Carrier', value: 2 },
    ],
    updateFilters: (filterContainer) => $(filterContainer).find("#filter-date-interval").show(),
    setupFilters: () => {}
}

function build(data, type) {
    if (data.min !== null && data.max != null) {
        $('#filter-interval-min').val(data.min);
        $('#filter-interval-max').val(data.max);
    }

    switch (data.layout) {
        case 0:
            return buildIntervalTable(data, { type, });
        case 2:
            return buildIntervalsByCarrierTable(data, { type });
        case 3:
            return buildMatrixByIntervalsAndLane(data);
    }

    return null;
}

function updateFilters(filterContainer) {
    $(filterContainer).find("#filter-date-interval, #filter-interval-options").show();
}

function setDefaultIntervalFilters(container, max, intervalOptions, defaultInterval) {
    $(container).find("#filter-interval-min").data('default', 0);
    $(container).find("#filter-interval-max").data('default', max);

    const $select = $(container).find('#filter-interval-size').data('default', null).empty();
    intervalOptions.forEach(e => {
        $select.append($('<option>').text(e).val(e));
    });

    if (defaultInterval) {
        $select.data('default', defaultInterval);
    }
    $select.selectpicker('refresh');
}

const columns = [
    { text: '', defaultSort: true, target: null, clickable: true },
    { text: 'Shipments', target: 'shipments', hide: true, clickable: true },
    { text: 'Qty', target: 'qty', hide: true, clickable: true },
    { text: 'Weight', target: 'kg', hide: true, clickable: true },
    { text: 'Cbm', target: 'cbm', hide: true, clickable: true },
    { text: 'Ldm', target: 'ldm', hide: true, clickable: true },
    { text: 'Freight', target: 'freightPrice', hide: true, clickable: true },
    { text: 'Total', target: 'totalPrice', clickable: true },
];

// Layout 0
function buildIntervalTable(data, { type = 'interval' } = {}) {
    columns[0].text = type;
    let rows = [];
    
    function buildTable(parent) {
        let currentSortedColumn = null;
        let asc = true;
        const table = createTableWithHeader(columns, (tableElement, e, target) => {
            if (currentSortedColumn === target) {
                asc = !asc;
            } else {
                asc = true;
                currentSortedColumn = target;
            }

            e.closest('tr')?.querySelector('span.fa')?.remove();
            e.append($('<span>').addClass(`fa fa-arrow-${asc ? 'down' : 'up'} ml-1`).css('font-size', 'x-small')[0]);
            tableElement.querySelector('tbody').replaceWith(createTBody(currentSortedColumn, asc));
        });

        const [contentHeader, closeFieldsModal] = createContentHeader(table, rows, data.result.total, columns, 'mb-2');
        table.append(createTBody());
        parent.append(contentHeader, table);
        parent.onclick = closeFieldsModal;

        // function for creating or sorting table body based on specific column
        function createTBody(column = '', asc = true) {
            // cache all elements and data on the first render
            if (rows.length === 0) {
                let index = 0;
                for (const interval of data.result.intervals) {
                    const intervalString = createIntervalString(interval, type);
                    const rowObj = createRow({
                        firstElement: $('<th>').append(intervalString),
                        data: interval,
                        columns: columns,
                    });

                    rows.push({ ...rowObj, index: index++ });
                }
            } else {
                if (column) {
                    rows = rows.sort((a, b) => {
                        if (a.data[column].value === b.data[column].value)
                            return asc ? a.index - b.index : b.index - a.index
                        
                        
                        return asc 
                            ? b.data[column].value - a.data[column].value 
                            : a.data[column].value - b.data[column].value;
                    });
                } else {
                    rows = rows.sort((a, b) => asc ? (a.index - b.index) : (b.index - a.index));
                }
            }

            const body = create('tbody');
            rows.forEach(e => body.append(e.element));
            return body;
        }
    }

    const container = create('figure')
    buildTable(container);
    return [container];
}

// Layout 2
function buildIntervalsByCarrierTable(data, { type = 'interval' } = {}) {
    columns[0].text = 'Carrier';
    let carrierGroups = [];
    
    function buildTable(parent) {
        let currentSortedColumn = null;
        let asc = true;
        const table = createTableWithHeader(columns, (tableElement, e, col) => {
            if (currentSortedColumn === col) {
                asc = !asc;
            } else {
                asc = true;
                currentSortedColumn = col;
            }

            e.closest('tr')?.querySelector('span.fa')?.remove();
            e.append($('<span>').addClass(`fa fa-arrow-${asc ? 'down' : 'up'} ml-1`).css('font-size', 'x-small')[0]);
            setTimeout(() => {
                tableElement.querySelectorAll('tbody').forEach(e => e.remove());
                tableElement.append(...createBody(currentSortedColumn, asc));
            }, 5);
        });

        const [contentHeader, closeFieldsModal] = createContentHeader(table, carrierGroups, data.result.total, columns, 'mb-2');
        table.append(...createBody());
        parent.append(contentHeader, table);
        parent.onclick = closeFieldsModal;
        
        function createBody(column = '', asc = true) {
            if (carrierGroups.length === 0) {
                let index = 0;
                for (const carrier of data.result.carriers) {
                    const rows = [];

                    // Creating interval objects for current carrier
                    for (const interval of carrier.intervals) {
                        const intervalString = createIntervalString(interval, type);
                        const rowObj = createRow({
                            firstElement: $('<td>').text(intervalString),
                            data: interval,
                            columns: columns,
                        });

                        rows.push({ ...rowObj, index: ++index });
                    }

                    // Creating carrier group object
                    const $carrierIntervalsBody = $('<tbody>').hide();
                    const toggleButton = createExpandButton(() => $carrierIntervalsBody.toggle());
                    const carrierRowObj = createRow({
                        firstElement: $('<th>').append([toggleButton, carrier.carrier]),
                        data: carrier.total,
                        columns: columns,
                    });

                    carrierGroups.push({ 
                        ...carrierRowObj, 
                        innerRows: rows,
                        intervalsBody: $carrierIntervalsBody.get(0),
                        index: ++index,
                    });
                }
            } else {
                if (column) {
                    const sort = (a, b) => {
                        if (a.data[column].value === b.data[column].value) 
                            return asc ? a.index - b.index : b.index - a.index
                        
                        return asc
                            ? b.data[column].value - a.data[column].value
                            : a.data[column].value - b.data[column].value
                    }
                    
                    carrierGroups.forEach(e => {
                        e.innerRows = e.innerRows.sort(sort); 
                    });

                    carrierGroups = carrierGroups.sort(sort);
                } else {
                    carrierGroups.forEach(e => {
                        e.innerRows = e.innerRows.sort((a, b) => asc ? (a.index - b.index) : (b.index - a.index))
                    });
                    carrierGroups = carrierGroups.sort((a, b) => asc ? (a.index - b.index) : (b.index - a.index));
                }
            }

            const elements = [];
            carrierGroups.forEach(e => {
                const carrierBody = $('<tbody>').append(e.element).addClass('sticky-body');
                empty(e.intervalsBody);
                
                e.innerRows.forEach(i => e.intervalsBody.append(i.element));
                elements.push(carrierBody.get(0), e.intervalsBody);
            });
            
            return elements;
        }
    }
    
    const container = create('figure')
    buildTable(container);
    
    return [container];
}

// ============================================= Utility Functions =============================================
function createContentHeader(table, rows, total, columns, classes = '') {
    const $header = $('<div>').addClass('d-flex position-relative justify-content-end ' + classes);
    
    function toggleAverageView(obj, checked) {
        for (const [key, val] of Object.entries(obj)) {
            if (checked) {
                if (key === 'shipments') {
                    val.element.innerText = 1;
                    continue;
                }
                const average = val.value / obj.shipments.value;
                val.element.innerText = formatDecimal(average, { autoFormat: true });
            } else {
                val.element.innerText = formatDecimal(val.value, { autoFormat: true });
            }
        }
    }
    
    function togglePercentView(obj, checked) {
        for (const [key, val] of Object.entries(obj)) {
            if (checked) {
                const percent = val.value / total[key] * 100;
                val.element.innerText = formatDecimal(percent, { min: 2, max: 2 }) + '%';
            } else {
                val.element.innerText = formatDecimal(val.value, { autoFormat: true });
            }
        }
    }

    let percentViewActive = false;
    let averageViewActive = false;
    let timeout = null;
    
    const averageToggle = createToggleButton('Average', (e) => {
        clearTimeout(timeout);
        const checked = e.checked;
        $header.find('#percent-toggle-button').get(0).checked = false;
        percentViewActive = false;

        timeout = setTimeout(() => {
            if (checked === averageViewActive) return;
            rows.forEach(e => {
                toggleAverageView(e.data, checked);
                e.innerRows?.forEach(i => toggleAverageView(i.data, checked));
            });

            averageViewActive = checked;
        }, 300);
    }, false, 'average-toggle-button');
    
    const percentsToggle = createToggleButton('Percents', (e) => {
        clearTimeout(timeout);
        const checked = e.checked;
        $header.find('#average-toggle-button').get(0).checked = false;
        averageViewActive = false;

        timeout = setTimeout(() => {
            if (checked === percentViewActive) return;
            rows.forEach(e => {
                togglePercentView(e.data, checked)
                e.innerRows?.forEach(i => togglePercentView(i.data, checked));
            });

            percentViewActive = checked;
        }, 300);
    }, false, 'percent-toggle-button');
    
    const $fieldsModal = $('<div>')
        .addClass('modal-fields')
        .append(createFieldToggleButtons(columns, rows, table))
        .click(e => e.stopPropagation())
        .hide();

    const $openFieldsModalButton = $('<button>').addClass('btn btn-primary').text('Fields').click(e => {
        e.stopPropagation();
        $fieldsModal.fadeToggle(100);
        e.currentTarget.classList.toggle('pressed');
    });

    const closeFieldsModal = () => {
        $fieldsModal.fadeOut();
        $openFieldsModalButton.removeClass('pressed');
    }
    
    $header.append([$fieldsModal, averageToggle, percentsToggle, $openFieldsModalButton]);
    return [$header.get(0), closeFieldsModal];
}