import { laneIntervals, kgIntervals, qtyIntervals, cbmIntervals, ldmIntervals } from "./reports/intervals.js";
import AdditionalCosts from "./reports/additionalCosts.js";
import { fetchData } from "./helpers.js";

const reports = {
    lane: laneIntervals,
    kg: kgIntervals,
    qty: qtyIntervals,
    cbm: cbmIntervals,
    ldm: ldmIntervals,
    ac: AdditionalCosts,
}

let currentReport = null;
let controller = null;
const baseUrl = document.querySelector('#statistics-data-url').value.replace(/index/gi, "");
const contentContainer = document.querySelector('#statistics-content');

$(document).ready(() => {
    init();
});

async function init() {
    let report = new URLSearchParams(window.location.search).get('report')?.toLowerCase();
    currentReport = reports[report];
    if (currentReport === undefined) {
        currentReport = Object.values(reports)[0];
        report = Object.keys(reports)[0];
    }
    
    // Hydration of the static elements
    $('.expand-button').click(({ currentTarget: target }) => {
        target.textContent = target.textContent === 'open_in_full' ? 'close_fullscreen' : 'open_in_full';
        $('#Panels').toggle();
    });
    
    $('#reset-filters-button').click(resetFilters);
    const $applyButton = $('#apply-filters-button').click((e) => {
        $(e.currentTarget).addClass('disabled');
        updateTable();
    });

    $('input[type="checkbox"]').on('input', () => $applyButton.removeClass('disabled'))
    $('.filter-input').on('input', () => $applyButton.removeClass('disabled'));
    $('.selectpicker').on('changed.bs.select', () => $applyButton.removeClass('disabled'));
    $('.datepicker').datetimepicker({
        format: 'DD-MM-YYYY',
        ignoreReadonly: true,
        keepOpen: false,
        useCurrent: false,
        showClear: true
    }).on('dp.change', (e) => {
        $applyButton.removeClass('disabled');
    });

    $('#report-select').on('changed.bs.select', ({ currentTarget: target }) => {
        const url = new URL(window.location);
        url.searchParams.set('report', $(target).val());
        window.history.replaceState({}, null, url.href);

        currentReport = reports[$(target).val()];
        reset("Update Report", true);
    });
    
    $('#layout-select').on('changed.bs.select', (e) => {
        reset("Update Layout", false, false);
    });

    $('#report-select').val(report).selectpicker('refresh').trigger('change');
}

async function updateTable() {
    setLoading();
    controller = new AbortController();

    try {
        const json = await fetchData(baseUrl + currentReport?.endPoint, getFiltersObject(), controller);
        const [table, func] = currentReport.build(json);
        updateContent(table);
        func && func();
    } catch(error) {
        console.error(error);
        console.error(error.message);

        showUpdateButton('Try Again');
        const errorMessage = $('<p class="w-100 text-center text-danger">Something went wrong</p>').get(0);
        updateContent(errorMessage, { prepend: true });
    }
}

function reset(buttonText, resetLayout = false, resetFilter = true) {
    controller?.abort();
    if (resetLayout) {
        currentReport.setupFilters($('#statistics-filter').get(0));
        updateLayoutSection();
    }

    updateFilterSection();
    resetFilter && resetFilters();
    showUpdateButton(buttonText);
}

function getFiltersObject() {
    const filterObj = {};
    $('#statistics-filter .selectpicker, #statistics-filter .datepicker, #statistics-filter .filter-input, #layout-panel .selectpicker').each((i, e) => {
        filterObj[$(e).prop('name')] = $(e).val();
    });
    
    $('#statistics-filter input[type="checkbox"]').each((_, e) => {
       filterObj[$(e).prop('name')] = e.checked; 
    });
    
    return filterObj;
}

function resetFilters() {
    $('.selectpicker[name="Year"], .selectpicker[name="IntervalSize"]').each((_, e) => {
        const defaultVal = $(e).data('default');
        if (defaultVal) {
            $(e).selectpicker('val', defaultVal);
        } else {
            $(e).selectpicker('val', e.options[0]?.value)
        } 
    });

    $('#statistics-filter input[type="checkbox"]').each((_, e) => {
        e.checked = false;
    });

    $('.filter-input').each((_, e) => $(e).val($(e).data('default')));
    $('.datepicker').each((_, e) => $(e).data('DateTimePicker').clear());
    $('.selectpicker').each((_, e) => {
        if ($(e).data('selectpicker').$searchbox.val()?.length > 0) {
            $(e).data('selectpicker').$searchbox.val('')
            $(e).selectpicker('refresh');
        }
        
        $(e).data('selectpicker').deselectAll();
    });
}

function updateFilterSection() {
    const $container = $('#statistics-filter');
    $container.find('.filter-optional').hide();
    currentReport?.updateFilters($container.get(0), Number($('#layout-select').val()));
}

function updateLayoutSection() {
    const $select = $('#layout-select');
    $select.find('option').remove();

    currentReport.layout?.forEach(e => {
        $select.append($('<option>').val(e.value).text(e.description));
    });
    
    $select.selectpicker('refresh');
}

function showUpdateButton(text) {
    const $button = $('<button>').text(text).addClass('btn btn-primary').on('click', () => {
        updateTable();
    });

    const $container = $('<div>').addClass('text-center mt-3').append($button);
    updateContent($container.get(0));
}

function setLoading() {
    updateContent($('<div class="w-100 text-center mt-3"><i class="fa fa-3x fa-spinner fa-spin"></i></div>').get(0));
}

function updateContent(child, settings = {}) {
    if(settings?.prepend === true) {
        contentContainer.prepend(child);
        return;
    }

    if(settings?.append === true) {
        contentContainer.appendChild(child);
        return;
    }

    while (contentContainer.firstChild)
        contentContainer.firstChild.remove();

    contentContainer.appendChild(child);
}