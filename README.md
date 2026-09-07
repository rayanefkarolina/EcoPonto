# EcoPonto Local

Aplicação Civic Tech para mapeamento de pontos de descarte consciente (Backend .NET 10 + Frontend Angular).

Rodando localmente (backend)

1. Entre na pasta do backend e execute:

```bash
cd EcoPontoBackend
dotnet restore
dotnet build
dotnet run --project EcoPonto.API
```

Por padrão a API rodará em `http://localhost:5000` (ou outra porta mostrada no console). A API cria/usa um SQLite `ecoponto.db` se não for configurado `ConnectionStrings__DefaultConnection`.

Rodando localmente (frontend)

1. Entre na pasta do frontend e instale dependências:

```bash
cd EcoPontoFrontend
npm install
npm start
```

O `npm start` usa `proxy.conf.json` para encaminhar `/api` ao backend em `http://localhost:5000`.

Docker (backend)

```bash
docker build -t ecoponto-backend EcoPontoBackend
docker run -e ConnectionStrings__DefaultConnection="Data Source=ecoponto.db" -p 5000:80 ecoponto-backend
```

Notas
- API pública: sem autenticação (conforme requisito).
- Banco padrão: Postgres se `ConnectionStrings__DefaultConnection` apontar para um Postgres; caso contrário, SQLite local (`ecoponto.db`).
- Frontend usa Leaflet + OpenStreetMap (sem chaves externas).
