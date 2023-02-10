import {
    create,
    createDelayedToggleButton,
    createExpandButton,
    createToggleButton,
    empty,
    formatDecimal, syncScrollbars
} from "../helpers.js";
import {createRow, createTableWithHeader} from "../components/tableBuilder.js";

const layout = [
    {description: "By Carrier", value: 2},
    {description: "By Lane", value: 4},
    {description: "Annual (By Carrier)", value: 1}
]

export default {
    endPoint: "AdditionalCosts",
    build: build,
    layout: layout,
    updateFilters: updateFilters,
    setupFilters: () => {}
};

function build(data) {
    switch (data.layout) {
        case 1:
            return buildAdditionalCostsByMonthTable(data);
        case 2:
            return buildAdditionalCostsTable(data, { type: "carriers" });
        case 4:
            return buildAdditionalCostsTable(data, { type: "lanes" });
    }

    return null;
}

function updateFilters(filterContainer, selectedLayout) {
    switch (selectedLayout) {
        case 1:
            $(filterContainer).find("#filter-date-year").show();
            break;
        case 2:
        case 4:
            $(filterContainer).find("#filter-date-interval").show();
            break;
    }
}

// ----------------------------- By Carrier/Lane Layout -----------------------------

function buildAdditionalCostsTable(data, { type = 'intervals' } = {}) {
    let groups = [];
    const columns = [
        { text: (type === 'lanes' ? 'Lane' : 'Carrier'), defaultSort: true, target: null, clickable: true },
        { text: (type === 'lanes' ? 'Carrier' : ''), target: 'carrier', showOnly: true, classes: 'text-left' },
        { text: 'Count', target: 'count', clickable: true },
        { text: 'Price', target: 'price', clickable: true }
    ];

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
            setTimeout(() => {
                tableElement.querySelectorAll('tbody').forEach(e => e.remove());
                tableElement.append(...createBody(currentSortedColumn, asc));
            }, 5);
        });
        
        const contentHeader = createContentHeader(groups, data.result.total);
        table.append(...createBody());
        parent.append(contentHeader, table);
        
        function createBody(column = '', asc = true) {
            if (groups.length === 0) {
                let index = 0;
                for (const element of data.result[type]) {
                    const rows = [];
                    
                    // Freight
                    const freightObj = createRow({
                        firstElement: $('<td>').text('Freight'),
                        data: element.freight,
                        columns : columns,
                    });

                    rows.push({ ...freightObj, index: -1 });

                    // Additional Costs for current carrier/lane
                    for (const ac of element.additionalCosts) {
                        const tdDescription = ac.href
                            ? $('<a>').prop('href', ac.href).prop('target', '_blank').text(ac.additionalCost)
                            : ac.additionalCost;

                        const acRowObj = createRow({
                            firstElement: $('<td>').append(tdDescription),
                            data: { ...ac.data, carrier: ac.carrier },
                            columns: columns
                        });

                        rows.push({ ...acRowObj, index: ++index });
                    }

                    // Carrier/Lane description and total values
                    const $additionalCostTBody = $('<tbody>').hide();
                    const toggleButton = createExpandButton(() => $additionalCostTBody.toggle());
                    
                    const description = type === 'lanes' ? `${element.lane.senderCountryISO} - ${element.lane.receiverCountryISO}` : element.carrier;
                    const targetRowObj = createRow({
                        firstElement: $('<th>').append([toggleButton, description]),
                        data: element.total,
                        columns: columns,
                    });

                    groups.push({
                        ...targetRowObj,
                        innerRows: rows,
                        dataBody: $additionalCostTBody.get(0),
                        index: ++index,
                    });
                }
            } else {
                if (column) {
                    const sort = (a, b) => asc
                        ? b.data[column].value - a.data[column].value
                        : a.data[column].value - b.data[column].value

                    groups.forEach(e => {
                        e.innerRows = e.innerRows.sort(sort);
                    });
                    
                    groups = groups.sort(sort);
                } else {
                    groups = sortByIndex(groups, asc);
                }
            }
            
            const elements = [];
            groups.forEach(e => {
                const groupTBody = $('<tbody>').append(e.element).addClass('sticky-body');
                empty(e.dataBody);

                e.innerRows.forEach(i => e.dataBody.append(i.element));
                elements.push(groupTBody.get(0), e.dataBody);
            })

            return elements;
        }
    }
    
    const container = create('figure');
    buildTable(container);
    
    return [container];
}

function createContentHeader(collection, total) {
    const togglePercentView = (obj, checked) => {
        for (const [key, val] of Object.entries(obj)) {
            if (checked) {
                const percent = val.value / total[key] * 100;
                val.element.innerText = formatDecimal(percent, { min: 2, max: 2 }) + '%';
            } else {
                val.element.innerText = formatDecimal(val.value, { autoFormat: true });
            }
        }
    }

    const togglePercentButton = createDelayedToggleButton({ 
        title: 'Percents',
        delay: 300,
        action: (checked) => {
            collection.forEach(i => {
                togglePercentView(i.data, checked)
                i.innerRows?.forEach(item => togglePercentView(item.data, checked));
            });
        }
    });
    
    return $('<div>').addClass('row d-flex justify-content-end mb-1').append(togglePercentButton).get(0);
}

//  ----------------------------- Month by month layout -----------------------------

function buildAdditionalCostsByMonthTable(data) {
    let groups = [];
    
    const table = $('<table>').addClass('by-month-table table table-striped table-hover hide-count-field').get(0);
    table.append(...buildOrSortBodyContent(groups, data));

    let currentSortedMonth = null;
    let asc = true;
    
    const [monthTable, monthRow] = createMonthlyHeader(data.result, 'Carrier', (e, month) => {
        if (currentSortedMonth === month) {
            asc = !asc;
        } else {
            asc = true;
            currentSortedMonth = month;
        }

        e.closest('tr')?.querySelector('span.fa')?.remove();
        e.append($('<span>').addClass(`fa fa-arrow-${asc ? 'down' : 'up'} ml-1`).css('font-size', 'x-small')[0]);
        setTimeout(() => {
            empty(table);
            table.append(...buildOrSortBodyContent(groups, data, currentSortedMonth, asc));
        }, 50);
    });
    
    const contentHeader = createMonthlyContentHeader(table, groups);
    const $headerContainer = $('<div>').append(monthTable).addClass('by-month-header-container sticky-top');
    const $bodyContainer = $('<div>').append(table).addClass('by-month-body-container');
    const $contentContainer = $('<div>').addClass('by-month-container').append([$headerContainer, $bodyContainer]);
    
    syncScrollbars($headerContainer[0], $bodyContainer[0]);
    new ResizeObserver(() => syncWidth(monthRow, table)).observe($contentContainer.get(0));
    
    const container = $('<figure>').append(contentHeader, $contentContainer);
    return [container.get(0)];
}

function buildOrSortBodyContent(groups, data, month = null, asc = true) {
    if (groups.length === 0) {
        let index = 0;
        for (const carrier of data.result.carrierGroups) {
            const rows = [];

            // Freight stats
            const freightObj = createMonthByMonthRowGroup({
                firstElement: $('<td>').text('Freight').addClass('sticky-left'),
                fiscalYearStart: data.result.fiscalYearStart,
                data: carrier.freightByMonth
            });

            rows.push({ ...freightObj, index: -1 });

            // Additional Costs
            for (const ac of carrier.additionalCosts) {
                const acRowObj = createMonthByMonthRowGroup({
                    firstElement: $('<td>').text(ac.description).addClass('sticky-left'),
                    fiscalYearStart: data.result.fiscalYearStart,
                    data: ac.statsByMonth
                });

                rows.push({ ...acRowObj, index: ++index });
            }

            // Carrier total stats
            const $additionalCostTBody = $('<tbody>').hide()
            const toggleButton = createExpandButton(() => {
                $additionalCostTBody.toggle();
            });

            const carrierObj = createMonthByMonthRowGroup({
                firstElement: $('<td>').append([toggleButton, carrier.carrier]).addClass('sticky-left text-bald'),
                fiscalYearStart: data.result.fiscalYearStart,
                data: carrier.totalByMonth,
            });

            groups.push({
                ...carrierObj,
                innerRows: rows,
                dataBody: $additionalCostTBody.get(0),
                index: ++index,
            });
        }
    } else {
        if (month !== null || month !== undefined) {
            const sort = (a, b) => {
                const month_a = a.data[month];
                const month_b = b.data[month];

                if (month_a === undefined && month_b === undefined) {
                    return asc ? (a.index - b.index) : (b.index - a.index);
                }

                return asc
                    ? (month_b?.price.value ?? -1) - (month_a?.price.value ?? -1)
                    : (month_a?.price.value ?? Infinity) - (month_b?.price.value ?? Infinity)
            }

            groups = groups.sort(sort);
            groups.forEach(e => {
                e.innerRows = e.innerRows.sort(sort);
            });
        } else {
            groups = sortByIndex(groups, asc);
        }
    }

    const elements = [];
    groups.forEach(e => {
        const carrierBody = $('<tbody>').append(e.element).addClass('carrier-body');
        empty(e.dataBody);

        e.innerRows.forEach(i => e.dataBody.append(i.element));
        elements.push(carrierBody.get(0), e.dataBody);
    });

    return elements;
}

function createMonthlyHeader(result, title, action) {
    const $headerTable = $('<table>').addClass('by-month-header-table');

    const colspan = 12 - result.fiscalYearStart
    const $yearRow = $('<tr>').append([
        $('<th>'),
        $('<th>').attr('colspan', colspan).text(result.year).addClass('year-data')
    ]);
    
    if (result.fiscalYearStart > 0)
        $yearRow.append($('<th>').attr('colspan', 12 - colspan).text(++result.year).addClass('year-data'));
    
    const $description = $('<th>')
        .text(title?.toUpperCase())
        .addClass('header-column sticky-left')
        .click((e) => action(e.currentTarget, null))
        .append($('<span>').addClass(`fa fa-arrow-down ml-1`).css('font-size', 'x-small'));
    
    const $row = $('<tr>').append($description).addClass('months-row');
    for (let i = 0; i < 12; i++) {
        const date = new Date();
        const month = (i + result.fiscalYearStart) % 12;
        date.setMonth(month);
        const monthString = date.toLocaleString('en-US', {month: 'short'});

        const th = $('<th>')
            .text(monthString.toUpperCase())
            .addClass('header-column')
            .click((e) => action(e.currentTarget, month));

        $row.append(th);
    }
    
    $headerTable.append($('<thead>').append($yearRow, $row));
    return [$headerTable.get(0), $row.get(0)];
}

function createMonthlyContentHeader(table, elements) {
    const togglePercentButton = createDelayedToggleButton({
        title: 'Percents',
        action: (checked) => {
            toggleMonthlyPercentView(elements, checked);
        }
    });
    
    const toggleCountButton = createToggleButton('Count', (target) => {
        table.classList.toggle('hide-count-field', !target.checked);
    });

    return $('<div>')
        .addClass('d-flex justify-content-end mb-2 gap-1')
        .append([toggleCountButton, togglePercentButton]).get(0);
}

function createMonthByMonthRowGroup({fiscalYearStart, data, firstElement }) {
    const obj = {};
    const $row = $('<tr>').append(firstElement);

    let prevStats = null;
    for (let i = 0; i < 12; i++) {
        const index = (i + fiscalYearStart) % 12;
        const monthData = data[index];
        const $td = $('<td>');

        if (monthData) {
            const priceSpan = $('<span>').text(formatDecimal(monthData.price, { autoFormat: true })).addClass('d-block');
            const countSpan = $('<span>').text(formatDecimal(monthData.count)).addClass('count-data d-block ac-border-top');
            $td.append([priceSpan, countSpan]);
            
            const monthObj = {
                price: {
                    element: priceSpan.get(0),
                    value: monthData.price,
                },
                count: {
                    element: countSpan.get(0),
                    value: monthData.count,
                },
            }

            if (prevStats) {
                const priceDiff = monthData.price - prevStats.price;
                monthObj.price.diff = priceDiff;
                monthObj.price.percent = priceDiff === 0 ? 0 : priceDiff / prevStats.price * 100;
                const countDiff = monthData.count - prevStats.count;
                monthObj.count.diff = countDiff;
                monthObj.count.percent = countDiff === 0 ? 0 : countDiff / prevStats.count * 100;
            }
            
            obj[index] = monthObj;
        }

        prevStats = monthData;
        $row.append($td);
    }
    
    return {
        element: $row.get(0),
        data: obj
    }
}

function toggleMonthlyPercentView(elements, toggle) {
    const togglePercent = (obj) => {
        for (const [_, value] of Object.entries(obj)) {
            for (const [key, val] of Object.entries(value)) {
                empty(val.element);
                val.element.style.borderTop = null;
                
                if (toggle) {
                    const percentElement = createPercentViewElement(val.percent, val.diff);
                    val.element.append(createPercentViewElement(val.percent, val.diff) ?? '');
                    val.element.style.borderTop = percentElement === null ? 'none' : null;
                } else {
                    val.element.innerText = formatDecimal(val.value, { autoFormat: true });
                }
            }
        }
    }
    
    elements.forEach(row => {
        togglePercent(row.data);
        row.innerRows.forEach(i => togglePercent(i.data));
    });
}

function createPercentViewElement(percent, difference, addBorder) {
    if (percent === undefined || difference === undefined)
        return null;

    const isPositive = +difference >= 0;
    const $content = $('<span>')
        .addClass(`text-${isPositive ? 'success' : 'danger'}`)
        .append($('<span>').addClass(`text-nowrap d-block ${addBorder ? 'ac-border-top' : ''}`)
            .append($('<i>').addClass(`mr-1 fa fa-long-arrow-alt-${isPositive ? 'up' : 'down'}`))
            .append(formatDecimal(percent, {min: 2, max: 2}) + '%'))
        .append(formatDecimal(difference, {autoFormat: true}));

    return $content.get(0);
}

// Sync header container columns with body container columns
function syncWidth(header, table) {
    const headerElements = header.getElementsByTagName('th');
    table.querySelectorAll('tbody:first-child tr td').forEach((e, i) => {
        if (headerElements[i])
            headerElements[i].style.minWidth = e.clientWidth + 'px';
    });
}

// General Utility Functions
function sortByIndex(elements, asc) {
    elements.forEach(e => {
        e.innerRows = e.innerRows.sort((a, b) => {
            if (b.index === -1) return 1;
            return asc ? (a.index - b.index) : (b.index - a.index)
        })
    });
    return elements.sort((a, b) => asc ? (a.index - b.index) : (b.index - a.index));
}