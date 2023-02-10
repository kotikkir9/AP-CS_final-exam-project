import {createToggleButton, formatDecimal} from "../helpers.js";

export function createTableWithHeader(columns, action) {
    const $table = $('<table>').addClass('table table-striped table-hover stats-table');
    const $tr = $('<tr>');

    columns.forEach(e => {
        const $th = $('<th>').text(e.text?.toUpperCase()).addClass(e.classes)
        if (e.clickable === true)
            $th.addClass('header-column').click(i => action($table.get(0), i.currentTarget, e.target));

        if (e.hide === true)
            $th.addClass('hide-' + e.target);

        if (e.defaultSort === true)
            $th.append($('<span>').addClass(`fa fa-arrow-down ml-1`).css('font-size', 'x-small'));

        $tr.append($th);
    })

    $table.append($('<thead>').append($tr));
    return $table.get(0);
}

export function createRow({ firstElement, data, columns }) {
    const obj = {};
    const $row = $('<tr>').append(firstElement);

    columns.forEach(e => {
        if (e.target === null || e.target === undefined) return;

        if (e.showOnly === true) {
            $row.append($('<td>').text(data[e.target]).addClass(e.classes));
            return;
        }

        const tdData = {}
        const $td = $('<td>')
            .text(formatDecimal(data[e.target], { autoFormat: true }))
            .addClass(e.classes)
        tdData.element = $td.get(0);
        tdData.value = data[e.target];

        obj[e.target] = tdData;
        $row.append($td);
    });

    return {
        element: $row.get(0),
        data: obj,
    }
}

export function createFieldToggleButtons(columns, elements, container) {
    const buttons = [];
    columns.forEach(e => {
        if (e.hide !== true) return;
        const toggle  = createToggleButton(e.text, target => {
            let display = target.checked ? null : 'none';

            elements.forEach(i => {
                i.data[e.target].element.style.display = display;
                i.innerRows?.forEach(row => {
                    row.data[e.target].element.style.display = display;
                });
            })

            const columnHeader = container.querySelector(`thead tr th.hide-${e.target}`);
            if (columnHeader) columnHeader.style.display = display;
        }, true);

        buttons.push(toggle);
    });

    return buttons;
}