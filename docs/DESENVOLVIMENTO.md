# 🚀 GUIA DE DESENVOLVIMENTO — ONBOARDING

## 1. PRÉ-REQUISITOS

| Ferramenta | Versão Mínima | Instalação |
|---|---|---|
| .NET SDK | 9.0 | https://dotnet.microsoft.com/download |
| Node.js | 20 LTS | https://nodejs.org |
| Docker Desktop | 4.x | https://www.docker.com/products/docker-desktop |
| Git | 2.x | https://git-scm.com |
| IDE | VS Code ou Visual Studio 2022 | — |

---

## 2. CLONE E CONFIGURAÇÃO INICIAL

```powershell
# Clone do repositório
git clone https://github.com/Sandokanalves/Ellosaude.git
cd ClinicaProjeto

# Instalação das dependências do frontend
npm --prefix ElloSaudeWeb install
```

---

## 3. EXECUTAR O AMBIENTE COMPLETO (DOCKER)

A forma **recomendada** de desenvolvimento é via Docker Compose, que sobe API + frontend + SQL Server + RabbitMQ com um único comando:

```powershell
# Da raiz do projeto ClinicaProjeto
cd ElloSaudeAPI

# Copie e configure as variáveis de ambiente
copy .env.example .env
# Edite o .env com suas senhas de desenvolvimento

# Suba todos os serviços
docker compose up --build -d

# Verifique os containers
docker compose ps
```

**URLs disponíveis:**
- Frontend: `http://localhost:5173` (ou porta mapeada pelo Nginx)
- API: `http://localhost:7001` (com Swagger em `/swagger`)
- API Health: `http://localhost:7001/health`
- RabbitMQ Management: `http://localhost:15672` (guest/guest)

---

## 4. EXECUTAR LOCALMENTE (SEM DOCKER)

### 4.1. Backend

```powershell
cd ElloSaudeAPI

# Configurar variáveis de ambiente (ou appsettings.Development.json)
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=ElloSaude;..."
$env:Jwt__Key = "SuaChaveJWTDeDesenvolvimento256bits"
$env:Jwt__Issuer = "ElloSaude"
$env:Jwt__Audience = "ElloSaudeClient"

# Executar
dotnet run --project ElloSaude.Api
```

### 4.2. Frontend

```powershell
cd ElloSaudeWeb
npm run dev
```

Frontend disponível em `http://localhost:5173`.

---

## 5. CREDENCIAIS DE ACESSO (SEED INICIAL)

Após a primeira inicialização, o `DbInitializer` cria automaticamente:

| Usuário | E-mail | Senha (env `SEED_PASSWORD`) | Role |
|---|---|---|---|
| Admin | `admin@ellosaude.com` | Valor de `.env` | Admin |
| Médico | `doutor@ellosaude.com` | Valor de `.env` | Profissional |
| Secretária | `secretaria@ellosaude.com` | Valor de `.env` | Secretaria |

> A senha inicial é definida pela variável `SEED_PASSWORD` no `.env`. Troque após o primeiro acesso.

---

## 6. MIGRATIONS (BANCO DE DADOS)

As migrations são aplicadas automaticamente na inicialização via `MigrateAsync()`.  
Para criar uma nova migration manualmente:

```powershell
cd ElloSaudeAPI
dotnet ef migrations add <NomeDaMigration> \
  --project ElloSaude.Infrastructure \
  --startup-project ElloSaude.Api
```

Para aplicar manualmente:
```powershell
dotnet ef database update \
  --project ElloSaude.Infrastructure \
  --startup-project ElloSaude.Api
```

---

## 7. EXECUTAR OS TESTES

```powershell
cd ElloSaudeAPI
dotnet test ElloSaude.sln
```

Todos os testes devem passar (exceto o de RabbitMQ que requer o container ativo).

---

## 8. ESTRUTURA DO PROJETO

```
ClinicaProjeto/
├── ElloSaudeAPI/                    ← Backend .NET 9
│   ├── ElloSaude.Domain/            ← Entidades e regras de domínio
│   ├── ElloSaude.Application/       ← CQRS, Commands, Queries, Validators
│   ├── ElloSaude.Infrastructure/    ← EF Core, JWT, PDF, RabbitMQ
│   ├── ElloSaude.Api/               ← Controllers, Middleware, Program.cs
│   ├── ElloSaude.UnitTests/         ← Testes unitários
│   ├── ElloSaude.IntegrationTests/  ← Testes de integração
│   ├── docker-compose.yml           ← Orquestração Docker
│   ├── .env.example                 ← Template de variáveis de ambiente
│   └── README.md                    ← Guia de execução da API
├── ElloSaudeWeb/                    ← Frontend Vue 3
│   ├── src/
│   │   ├── api/                     ← Módulos Axios por domínio
│   │   ├── components/              ← Componentes reutilizáveis
│   │   ├── layouts/                 ← Layouts (sidebar adaptativo)
│   │   ├── pages/                   ← Páginas por módulo
│   │   ├── router/                  ← Roteamento com guards
│   │   └── stores/                  ← Pinia (auth)
│   └── vite.config.js
└── docs/                            ← Documentação técnica
    ├── ARQUITETURA.md
    ├── API_ENDPOINTS.md
    ├── MODELO_DE_DADOS.md
    ├── SEGURANCA_E_LGPD.md
    ├── REQUISITOS_FUNCIONAIS.md
    ├── TESTES.md
    ├── DOCKER.md
    ├── DEPLOY_PRODUCAO.md
    └── PLANO_DE_IMPLEMENTACAO.md
```

---

## 9. PADRÕES DE CÓDIGO

### Backend (C# / .NET)
- **Clean Architecture**: Domain → Application → Infrastructure → Api (dependências só para dentro).
- **CQRS**: Toda operação de escrita é um `Command`, toda leitura é uma `Query`.
- **Validação**: Sempre via `FluentValidation` na camada Application — nunca no controller.
- **Handlers**: Lançam exceções tipadas (`InvalidOperationException`, `KeyNotFoundException`, `UnauthorizedAccessException`), que o middleware traduz para ProblemDetails.
- **Naming**: PascalCase para classes, camelCase para variáveis, snake_case nunca.

### Frontend (Vue 3 / TypeScript)
- **Composition API** com `<script setup>` obrigatório.
- **Pinia** para estado global (não Vuex).
- **PrimeVue** para componentes de UI.
- Chamadas à API: sempre via módulos `src/api/*.js`, não direto nos componentes.

---

## 10. FLUXO DE TRABALHO GIT

```
# Sempre work em branch de feature
git checkout -b feat/nome-da-feature

# Commits semânticos
git commit -m "feat(patients): adiciona campo profissao no cadastro"

# Push e PR
git push origin feat/nome-da-feature
```

**Padrões de commit:** `feat | fix | docs | test | refactor | chore`

**Regra:** `dotnet test` deve passar antes de qualquer commit.
