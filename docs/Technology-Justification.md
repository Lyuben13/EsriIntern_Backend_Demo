# Обосновка за избора на технологии, библиотеки и алгоритми

## Технологии и библиотеки

### ASP.NET Core Web API (.NET 8)
**Избор**: ASP.NET Core Web API е използван като основна платформа за REST API услугата.

**Обосновка**:
- **Мултиплатформеност**: .NET 8 работи на Windows, Linux и macOS, което дава свобода на приложението да работи на различни платформи
- **Високи производителност**: ASP.NET Core е оптимизиран за високи натоварвания и асинхронна обработка
- **Вградена функционалност**: Предоставя Dependency Injection, конфигурация, логване, routing и middleware out-of-the-box
- **Swagger/OpenAPI**: Автоматично генериране на API документация чрез Swashbuckle
- **Стандартизация**: Следва REST принципите и HTTP стандартите

### BackgroundService + PeriodicTimer
**Избор**: BackgroundService клас с PeriodicTimer за периодично изпълнение на background processing.

**Обосновка**:
- **Стандартен подход**: BackgroundService е официалният .NET механизъм за long-running задачи
- **Интеграция с DI**: Пълна интеграция с ASP.NET Core Dependency Injection системата
- **Graceful shutdown**: Поддръжка на cancellation tokens за коректно спиране при shutdown
- **PeriodicTimer**: Ефективен механизъм за периодично изпълнение без излишни ресурси (въведен в .NET 6)
- **Изолиране на scope**: Използване на IServiceScopeFactory за правилно управление на scoped services (като DbContext)

### HttpClientFactory
**Избор**: HttpClientFactory чрез AddHttpClient() за HTTP комуникация с ArcGIS REST API.

**Обосновка**:
- **Избягване на socket exhaustion**: HttpClientFactory управлява пула от HTTP connections
- **Централизирана конфигурация**: Лесно конфигуриране на timeout, base address и други настройки
- **Retry политики**: Възможност за добавяне на Polly за retry логика (разширяемост)
- **Логване**: Автоматично логване на HTTP заявки чрез IHttpClientFactory
- **Best practices**: Следва препоръките на Microsoft за използване на HttpClient

### Entity Framework Core + SQLite
**Избор**: EF Core като ORM с SQLite като база данни.

**Обосновка**:
- **SQLite**:
  - **Локално съхранение**: Не изисква отделен database server, удобно за демо и development
  - **Файл-базирана**: Лесно backup и пренос на данните
  - **Нулева конфигурация**: Работи out-of-the-box без допълнителна инфраструктура
  - **Лесно смяна**: EF Core позволява лесно преминаване към PostgreSQL, SQL Server и др. чрез промяна на connection string
- **EF Core**:
  - **Code-first подход**: Дефиниране на модела чрез C# класове
  - **Migrations**: Поддръжка на database migrations за версиониране на схемата
  - **LINQ**: Изразими и типобезопасни заявки
  - **Performance**: Ефективни заявки с оптимизации като AsNoTracking() когато е необходимо
  - **Async/await**: Пълна поддръжка на асинхронни операции

### System.Text.Json
**Избор**: System.Text.Json за десериализация на JSON отговори от ArcGIS API.

**Обосновка**:
- **Вградена библиотека**: Част от .NET стандартната библиотека, без допълнителни зависимости
- **Високи производителност**: По-бърза от Newtonsoft.Json в повечето случаи
- **Memory efficient**: Работи със streams за големи отговори
- **Case-insensitive**: Лесно конфигуриране за case-insensitive property matching

## Алгоритми и техники

### Пагинация на ArcGIS Feature Service
**Техника**: Използване на `resultOffset` и `resultRecordCount` параметри за пагинация.

**Обосновка**:
- **ArcGIS специфично**: Стандартният подход за извличане на големи обеми данни от ArcGIS REST API
- **Ефективност**: Избягва извличане на всички данни наведнъж, което би било неефективно за големи datasets
- **Контролируем размер**: Позволява контрол над размера на всяка страница (настройва се чрез MaxRecordCount)
- **Обработка на exceededTransferLimit**: Проверка за флаг, който индикира че има още данни дори при по-малко записи от page size

### Агрегация на данни
**Алгоритъм**: In-memory агрегация чрез Dictionary<string, long> с групиране по STATE_NAME и сумиране на POPULATION.

**Обосновка**:
- **Простота**: Директна и разбираема имплементация
- **Ефективност**: O(n) сложност при едно обхождане на данните
- **Case-insensitive**: Използване на StringComparer.OrdinalIgnoreCase за игнориране на разлики в регистъра
- **Обработка на липсващи данни**: Проверка за null/empty стойности преди агрегация

**Алтернативи** (за бъдещи подобрения):
- SQL агрегация директно в заявката към ArcGIS (ако поддържа)
- Batch processing за много големи datasets
- Паралелна обработка с Parallel.ForEach за много данни

### Snapshot модел на данните
**Техника**: Запазване само на последния snapshot на данните (заместване на старите записи).

**Обосновка**:
- **Простота**: Лесна имплементация и поддръжка
- **Актуалност**: Винаги се показват най-новите данни
- **Минимално storage**: Не се натрупват исторически данни

**Разширяемост**:
- Може лесно да се добави история чрез отделна таблица с timestamps
- Може да се добави versioning система
- Може да се добави data retention политика

### Error Handling
**Подход**: Try-catch блокове с логване на грешки без прекъсване на приложението.

**Обосновка**:
- **Resilience**: Background worker продължава да работи дори при временни грешки
- **Observability**: Логване на всички грешки за debugging и мониторинг
- **Graceful degradation**: API endpoint-ите продължават да работят дори ако последното refresh е неуспешно

## Архитектурни решения

### Separation of Concerns
- **ArcGisCountiesClient**: Отговорен само за комуникация с външния API
- **DemographicsRefresher**: Отговорен за бизнес логиката на refresh операцията
- **DemographicsWorker**: Отговорен за периодичното изпълнение
- **StatsController**: Отговорен за HTTP endpoint-ите

### Dependency Injection
Използване на DI за:
- Лоос coupling между компонентите
- Лесно тестване (mock-ване на зависимости)
- Конфигуриране на services чрез appsettings.json

### Configuration Management
- Всички настройки са в appsettings.json
- Използване на IOptions pattern за типобезопасна конфигурация
- Лесно променяне на настройки без recompile
