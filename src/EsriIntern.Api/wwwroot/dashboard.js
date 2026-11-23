const search = document.querySelector('#search');
const sort = document.querySelector('#sort');
const results = document.querySelector('#results');
const table = document.querySelector('#table');
const status = document.querySelector('#status');
const reload = document.querySelector('#reload');
const stateColumn = document.querySelector('#state-column');
const populationColumn = document.querySelector('#population-column');
const numberFormat = new Intl.NumberFormat('bg-BG');
const dateFormat = new Intl.DateTimeFormat('bg-BG', { dateStyle: 'medium', timeStyle: 'short' });
const stateNames = new Map(Object.entries({
    'Alabama': 'Алабама',
    'Alaska': 'Аляска',
    'Arizona': 'Аризона',
    'Arkansas': 'Арканзас',
    'California': 'Калифорния',
    'Colorado': 'Колорадо',
    'Connecticut': 'Кънектикът',
    'Delaware': 'Делауеър',
    'District of Columbia': 'Окръг Колумбия',
    'Florida': 'Флорида',
    'Georgia': 'Джорджия',
    'Hawaii': 'Хаваи',
    'Idaho': 'Айдахо',
    'Illinois': 'Илинойс',
    'Indiana': 'Индиана',
    'Iowa': 'Айова',
    'Kansas': 'Канзас',
    'Kentucky': 'Кентъки',
    'Louisiana': 'Луизиана',
    'Maine': 'Мейн',
    'Maryland': 'Мериленд',
    'Massachusetts': 'Масачузетс',
    'Michigan': 'Мичиган',
    'Minnesota': 'Минесота',
    'Mississippi': 'Мисисипи',
    'Missouri': 'Мисури',
    'Montana': 'Монтана',
    'Nebraska': 'Небраска',
    'Nevada': 'Невада',
    'New Hampshire': 'Ню Хемпшър',
    'New Jersey': 'Ню Джърси',
    'New Mexico': 'Ню Мексико',
    'New York': 'Ню Йорк',
    'North Carolina': 'Северна Каролина',
    'North Dakota': 'Северна Дакота',
    'Ohio': 'Охайо',
    'Oklahoma': 'Оклахома',
    'Oregon': 'Орегон',
    'Pennsylvania': 'Пенсилвания',
    'Rhode Island': 'Роуд Айлънд',
    'South Carolina': 'Южна Каролина',
    'South Dakota': 'Южна Дакота',
    'Tennessee': 'Тенеси',
    'Texas': 'Тексас',
    'Utah': 'Юта',
    'Vermont': 'Върмонт',
    'Virginia': 'Вирджиния',
    'Washington': 'Вашингтон',
    'West Virginia': 'Западна Вирджиния',
    'Wisconsin': 'Уисконсин',
    'Wyoming': 'Уайоминг'
}));
const byName = (a, b) => a.displayName.localeCompare(b.displayName, 'bg');
const comparators = {
    name: byName,
    ascending: (a, b) => a.population - b.population || byName(a, b),
    descending: (a, b) => b.population - a.population || byName(a, b)
};
let states = [];

function render() {
    const query = search.value.trim().toLowerCase();
    const filtered = states.filter(state =>
        state.displayName.toLowerCase().includes(query) || state.stateName.toLowerCase().includes(query)
    );
    filtered.sort(comparators[sort.value]);

    const rows = filtered.map(state => {
        const row = document.createElement('tr');
        row.insertCell().textContent = state.displayName;
        row.insertCell().textContent = numberFormat.format(state.population);
        return row;
    });
    results.replaceChildren(...rows);
    table.hidden = rows.length === 0;
    stateColumn.removeAttribute('aria-sort');
    populationColumn.removeAttribute('aria-sort');
    if (sort.value === 'name') {
        stateColumn.setAttribute('aria-sort', 'ascending');
    } else {
        populationColumn.setAttribute('aria-sort', sort.value);
    }

    status.textContent = rows.length
        ? `Показани записи: ${numberFormat.format(rows.length)} от ${numberFormat.format(states.length)}`
        : 'Няма резултати за това търсене.';
}

async function load() {
    reload.hidden = true;
    status.textContent = 'Зареждане на данните…';
    try {
        const response = await fetch('/api/stats/states', { cache: 'no-cache' });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const data = await response.json();
        states = data.map(state => ({
            ...state,
            displayName: stateNames.get(state.stateName) ?? state.stateName
        }));
        if (states.length === 0) {
            status.textContent = 'Все още няма заредени данни. Провери отново след малко.';
            reload.hidden = false;
            return;
        }

        const population = states.reduce((sum, state) => sum + state.population, 0);
        // SQLite can return UTC timestamps without a timezone suffix.
        const date = new Date(states[0].retrievedAtUtc + (
            /(?:Z|[+-]\d{2}:\d{2})$/i.test(states[0].retrievedAtUtc) ? '' : 'Z'
        ));
        document.querySelector('#state-count').textContent = numberFormat.format(states.length);
        document.querySelector('#total').textContent = numberFormat.format(population);
        document.querySelector('#updated').textContent = dateFormat.format(date);
        document.querySelector('#data-note').hidden = !states.some(state => state.population === 0);
        search.disabled = false;
        sort.disabled = false;
        render();
    } catch {
        status.textContent = 'Данните не можаха да се заредят.';
        reload.hidden = false;
    }
}

search.addEventListener('input', render);
sort.addEventListener('change', render);
reload.addEventListener('click', load);
load();
