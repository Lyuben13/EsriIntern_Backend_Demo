const citySearch = document.querySelector('#city-search');
const cityStatus = document.querySelector('#city-status');
const cityTable = document.querySelector('#city-table');
const cityResults = document.querySelector('#city-results');
const cityPages = document.querySelector('#city-pages');
const pageLabel = document.querySelector('#city-page');
const previous = document.querySelector('#city-prev');
const next = document.querySelector('#city-next');
const retry = document.querySelector('#city-retry');
citySearch.value = new URLSearchParams(window.location.search).get('q') ?? '';
const countFormat = new Intl.NumberFormat('bg-BG');
const pageSize = 25;
const cityAliases = new Map([
    ['Myrtle Beach city', 'Мъртъл Бийч'],
    ['North Myrtle Beach city', 'Норт Мъртъл Бийч Северен Мъртъл Бийч']
]);
let cities = [];
let matches = [];
let page = 0;

function normalize(value) {
    return value.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase();
}

function renderCities() {
    const first = page * pageSize;
    const pageCount = Math.ceil(matches.length / pageSize);
    const rows = matches.slice(first, first + pageSize).map(city => {
        const row = document.createElement('tr');
        row.insertCell().textContent = city.county ? `${city.name} · ${city.county}` : city.name;
        row.insertCell().textContent = city.state;
        row.insertCell().textContent = city.population === null ? '—' : countFormat.format(city.population);
        return row;
    });
    cityResults.replaceChildren(...rows);
    cityTable.hidden = rows.length === 0;
    cityPages.hidden = pageCount <= 1;
    previous.disabled = page === 0;
    next.disabled = page >= pageCount - 1;
    pageLabel.textContent = `Страница ${countFormat.format(page + 1)} от ${countFormat.format(pageCount)}`;
    cityStatus.textContent = rows.length
        ? `Показани ${countFormat.format(first + 1)}–${countFormat.format(first + rows.length)} от ${countFormat.format(matches.length)}`
        : 'Няма резултати за това търсене.';
}

function filterCities() {
    const words = normalize(citySearch.value.trim()).split(/\s+/).filter(Boolean);
    matches = cities.filter(city => words.every(word => city.searchText.includes(word)));
    page = 0;
    renderCities();
}

async function loadCities() {
    retry.hidden = true;
    cityStatus.textContent = 'Зареждане на данните…';
    try {
        const response = await fetch('/census-places-2025.json');
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const entries = await response.json();
        cities = entries.map(([name, englishState, population, county]) => {
            const state = stateNames.get(englishState) ?? englishState;
            const alias = cityAliases.get(name) ?? '';
            return { name, state, population, county, searchText: normalize(`${name} ${englishState} ${state} ${county ?? ''} ${alias}`) };
        });
        citySearch.disabled = false;
        filterCities();
    } catch {
        cityStatus.textContent = 'Данните за градовете не можаха да се заредят.';
        retry.hidden = false;
    }
}

citySearch.addEventListener('input', filterCities);
previous.addEventListener('click', () => {
    page -= 1;
    renderCities();
});
next.addEventListener('click', () => {
    page += 1;
    renderCities();
});
retry.addEventListener('click', loadCities);
loadCities();
