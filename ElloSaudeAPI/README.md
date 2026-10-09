# 🏥 ElloSaúde - Sistema de Gestão para Clínicas (SaaS Multi-Tenant)

O **ElloSaúde** é um ecossistema completo para gestão de clínicas médicas e de saúde, construído com arquitetura **SaaS Multi-Tenant** (isolamento total de dados por clínica), atendendo aos requisitos da **LGPD** e com alto foco em performance, testabilidade e experiência de usuário.

---

## 🛠️ Tecnologias Utilizadas

### Backend (.NET 9 Web API)
- **Framework**: .NET 9 C#
- **Arquitetura**: Clean Architecture + CQRS (Command Query Responsibility Segregation)
- **Mensageria & Eventos**: MediatR 14.x & RabbitMQ Client 7.x
- **Persistência**: Entity Framework Core 9.0 com SQL Server
- **Validação & Mapeamento**: FluentValidation & AutoMapper
- **Segurança**: ASP.NET Core JWT Bearer Authentication & BCrypt.Net-Next (Hashing de senhas com Salt)
- **Documentação da API**: Swashbuckle Swagger UI & Scalar API Reference
- **Testes**: xUnit, Moq, FluentAssertions & Microsoft.AspNetCore.Mvc.Testing (Integration Tests)

### Frontend (Vue 3 + Vite)
- **Framework**: Vue.js 3 (Composition API) com Vite
- **UI Kit**: PrimeVue 3, PrimeIcons & PrimeFlex
- **Calendário Interativo**: FullCalendar Vue 3 Plugin
- **Gerenciamento de Estado**: Pinia
- **Requisições HTTP**: Axios com Interceptors JWT e auto-logout 401
- **Validação de Formulários**: VeeValidate & Zod

### Infraestrutura
- **Containerização**: Docker & Docker Compose (SQL Server 2022 + RabbitMQ + API .NET 9 + frontend Nginx)

---

## 📂 Estrutura do Projeto Explicada Passo a Passo

```
ClinicaProjeto/
├── ElloSaudeAPI/                  # Solução Backend (.NET 9)
│   ├── ElloSaude.Domain/          # Entidades e Regras de Negócio Puras
│   ├── ElloSaude.Application/     # Casos de Uso (Commands, Queries, DTOs, Validators)
│   ├── ElloSaude.Infrastructure/  # EF Core DbContext, JWT, BCrypt, Repositórios, RabbitMQ
│   ├── ElloSaude.Api/             # ASP.NET Core Controllers, Middlewares, Program.cs
│   ├── ElloSaude.UnitTests/       # Suíte de Testes Unitários xUnit
│   ├── ElloSaude.IntegrationTests/# Suíte de Testes de Integração E2E
│   └── docker-compose.yml         # Orquestração Docker
└── ElloSaudeWeb/                  # Aplicação Frontend (Vue 3 + PrimeVue)
    ├── src/
    │   ├── api/                   # Serviços de integração Axios
    │   ├── components/            # Componentes Vue reutilizáveis
    │   ├── layouts/               # Layouts de autenticação e dashboard
    │   ├── pages/                 # Páginas (Agenda, Pacientes, Prontuário, Financeiro)
    │   ├── router/                # Vue Router com Navigation Guards
    │   └── stores/                # Pinia Auth Store (JWT claims)
    └── package.json
```

---

## 🧩 Detalhamento dos Componentes do Backend (`ElloSaudeAPI`)

### 1. Domain (`ElloSaude.Domain`)
Contém as entidades puras e enums de negócio, sem dependência de bibliotecas externas.
- **`BaseEntity.cs`**: Classe base que contém o identificador `Id` (Guid) e a propriedade `TenantId` (SaaS Multi-tenant).
- **`Patient.cs`**: Entidade Paciente (Nome, E-mail, CPF, Data de Nascimento e Histórico de Prontuários).
- **`Appointment.cs`**: Entidade Consulta/Agendamento (Data Início, Data Fim, PatientId, ProfessionalId, Status e Tipo).
- **`MedicalRecord.cs`**: Entidade Prontuário Eletrônico (PatientId, DoctorId, Diagnóstico, Descrição e Data de Criação).
- **`Clinic.cs`**: Entidade Clínica/Tenant (Nome, CNPJ, Razão Social).
- **`User.cs`**: Entidade Usuário do Sistema (E-mail, PasswordHash, Role, TenantId).
- **Enums**: `AppointmentStatus` (Pendente, Confirmado, Realizado, Cancelado) e `AppointmentType` (Consulta, Retorno, Exame).

### 2. Application (`ElloSaude.Application`)
Orquestra a lógica da aplicação usando o padrão CQRS via MediatR.
- **Interfaces (`Common/Interfaces/`)**:
  - `IApplicationDbContext.cs`: Abstração do DbContext.
  - `ITenantService.cs`: Provedor do ID do Tenant (clínica) atual.
  - `IUserService.cs`: Serviço para autenticação e obtenção do usuário logado.
  - `IHashService.cs`: Abstração de algoritmo seguro para senhas (BCrypt).
  - `IRepository<T>.cs` & `IUnitOfWork.cs`: Padrão Repositório e Unidade de Trabalho.
- **DTOs (`DTOs/`)**: Data Transfer Objects (`PatientDto`, `AppointmentDto`, `MedicalRecordDto`, `ClinicDto`, `FinancialSummaryDto`).
- **Commands & Handlers**:
  - `Patients/Commands`: `CreatePatientCommand`, `UpdatePatientCommand`, `DeletePatientCommand`.
  - `Appointments/Commands`: `CreateAppointmentCommand`, `UpdateAppointmentStatusCommand`, `DeleteAppointmentCommand`.
  - `MedicalRecord/Commands`: `CreateMedicalRecordCommand`, `AddMedicalRecordAddendumCommand`.
  - `Clinics/Commands`: `RegisterClinicCommand`.
- **Queries & Handlers**:
  - `Patients/Queries`: `GetAllPatientsQuery`, `GetPatientByIdQuery`.
  - `Appointments/Queries`: `GetAllAppointmentsQuery`, `GetAgendaByProfessionalQuery`, `GetAppointmentByIdQuery`.
  - `MedicalRecord/Queries`: `GetRecordsByPatientQuery`, `GetMedicalRecordByIdQuery`.
  - `Financial/Queries`: `GetFinancialSummaryQuery` e `GetFinancialReportQuery`.
  - `Financial/Commands`: registro de pagamento, retorno gratuito e cancelamento de lançamentos sem recebimento.
  - `Prescriptions`: emissão, consulta autorizada e geração de PDF.
  - `Professionals`: cadastro, disponibilidade semanal, slots e bloqueios da agenda.

### 3. Infrastructure (`ElloSaude.Infrastructure`)
Implementação técnica dos serviços de banco de dados, mensageria e segurança.
- **`ApplicationDbContext.cs`**: Contexto do EF Core com **Global Query Filter dinâmico por TenantId**, garantindo que nenhuma clínica veja dados de outra clínica.
- **`DbInitializer.cs`**: Seed demonstrativo opcional apenas em desenvolvimento; habilite `DevelopmentSeed:Enabled` e forneça `DevelopmentSeed:Password` por User Secrets ou variável de ambiente. Não há credenciais padrão.
- **`UserService.cs`**: Geração de tokens JWT seguros contendo claims (`NameIdentifier`, `Email`, `Role`, `TenantId`).
- **`HashService.cs`**: Hashing de senha com BCrypt com geração automática de sal.
- **`TenantService.cs`**: Extrai o `TenantId` do cabeçalho JWT da requisição HTTP atual.
- **`RabbitMqService.cs`**: Publicador de mensagens para filas assíncronas do RabbitMQ.

### 4. API (`ElloSaude.Api`)
Camada de exposição HTTP REST.
- **`Program.cs`**: Registro de injeção de dependências, configuração do JWT Bearer, CORS policy, Swagger com suporte a Authorize Header e pipeline HTTP.
- **`Middlewares/ExceptionMiddleware.cs`**: Middleware global para captura de exceções não tratadas e retorno de respostas JSON padronizadas (Status 500).
- **Controllers**:
  - `AuthController.cs`: Endpoints de autenticação (`POST /api/auth/login`).
  - `PatientsController.cs`: Endpoint de CRUD de Pacientes (`/api/patients`).
  - `AppointmentsController.cs`: Endpoints de Agenda e Status (`/api/appointments`).
  - `MedicalRecordsController.cs`: Prontuário confidencial exclusivo para médicos/admins (`/api/medicalrecords`).
  - `FinancialController.cs`: Painel de Faturamento (`/api/financial/summary`).
  - `ClinicsController.cs`: Cadastro de novas clínicas (`/api/clinics/register`).

---

## 🧪 Suíte de Testes Automatizados

Os testes automatizados cobrem regras de domínio e fluxos de API. Execute a suíte antes de publicar alterações; a quantidade de testes pode evoluir junto com o código.

### 1. Testes Unitários (`ElloSaude.UnitTests`)
- `PatientTests.cs`: Teste de criação e validação da entidade `Patient`.
- `AppointmentTests.cs`: Teste de valores padrão e criação da entidade `Appointment`.
- `CreatePatientHandlerTests.cs`: Teste isolado do handler MediatR `CreatePatientHandler` com mocks do `IUnitOfWork` e `ITenantService`.
- `HashServiceTests.cs`: Teste de geração e verificação de hash BCrypt.

### 2. Testes de Integração (`ElloSaude.IntegrationTests`)
- `AuthAndPatientsIntegrationTests.cs`: Teste ponta a ponta (E2E) utilizando `WebApplicationFactory<Program>` e banco em memória:
  1. Realiza login via `POST /api/auth/login`.
  2. Extrai o Token JWT retornado.
  3. Adiciona o cabeçalho `Authorization: Bearer {token}`.
  4. Cadastra um novo paciente via `POST /api/patients`.
  5. Consulta os pacientes cadastrados via `GET /api/patients` e valida a persistência e o filtro de tenant.

---

## 🎨 Detalhamento dos Componentes do Frontend (`ElloSaudeWeb`)

- **`src/api/axios.js`**: Cliente HTTP pré-configurado com suporte a env vars (`VITE_API_URL`), envio automático do JWT Token e interceptor 401.
- **`src/stores/auth.store.js`**: Store Pinia para gerenciamento de login/logout e decodificação das claims do token JWT.
- **`src/pages/LoginPage.vue`**: Formulário de login responsivo com feedback visual de erros e credenciais de teste pré-preenchidas.
- **`src/pages/agenda/AgendaPage.vue`**: Interface com **FullCalendar** conectada à API `/api/appointments` para reagendamento, alteração de status e novos agendamentos.
- **`src/pages/patients/PatientsListPage.vue`**: Tabela interativa (DataTable PrimeVue) com busca em tempo real, edição e exclusão de pacientes.
- **`src/pages/patients/PatientDetailPage.vue`**: Timeline de evoluções médicas (**Prontuário Eletrônico Confidencial**). Bloqueia a visualização para perfil Secretária conforme a **LGPD (RF10)**.
- **`src/pages/FinancialPage.vue`**: Painel de Faturamento exibindo cards métricos (Recebido, Pendente, Faturamento Total e percentual de consultas realizadas).

---

## 🚀 Como Rodar o Projeto Passo a Passo

### Pré-requisitos
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) instalado.
- [Node.js 18+](https://nodejs.org/) instalado.
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (opcional, para ambiente dockerizado).

---

### Passo 1: Executar a API (.NET 9)

1. Abra o terminal no diretório da API:
```powershell
cd "ClinicaProjeto/ElloSaudeAPI"
```

2. Execute o projeto API:
```powershell
dotnet run --project ElloSaude.Api
```

3. Acesse a documentação no navegador:
   - **Swagger UI**: [http://localhost:5080/swagger](http://localhost:5080/swagger)
   - **Scalar API Reference**: [http://localhost:5080/scalar/v1](http://localhost:5080/scalar/v1)

---

### Passo 2: Executar os Testes Automatizados

No mesmo diretório da API, execute:
```powershell
dotnet test ElloSaude.sln
```
> Os testes de integração usam banco isolado em memória; a inicialização via Docker também é validada separadamente contra SQL Server.

---

### Passo 3: Executar o Frontend (Vue 3)

1. Abra um novo terminal no diretório do Frontend:
```powershell
cd "ClinicaProjeto/ElloSaudeWeb"
```

2. Instale as dependências (se necessário):
```powershell
npm install
```

3. Inicie o servidor de desenvolvimento:
```powershell
npm run dev
```

4. Acesse a aplicação no navegador:
   - **URL**: [http://localhost:5173](http://localhost:5173)

---

### Passo 4: Executar o sistema completo no Docker Desktop

No PowerShell, a partir da raiz do repositório:

```powershell
Set-Location .\ElloSaudeAPI
Copy-Item .env.example .env
```

Edite `.env` e substitua todos os valores de exemplo por segredos aleatórios exclusivos. Não versione nem compartilhe esse arquivo. Se ele já existir, preserve os valores locais. Depois, valide e suba a stack:

```powershell
docker compose config --quiet
docker compose up --build -d
docker compose ps
```

O serviço da API aplica as migrations pendentes no SQL Server antes de ficar pronto; os healthchecks aguardam SQL Server e RabbitMQ. O frontend só inicia após a API estar saudável.

| Serviço | Endereço local |
| --- | --- |
| Aplicação web | <http://localhost:8088> |
| API e Swagger (Development) | <http://localhost:5080/swagger> |
| SQL Server | `localhost,1433` |
| RabbitMQ Management | <http://localhost:15672> |

As portas publicadas ficam limitadas a `127.0.0.1`. Para verificar diagnósticos, use `docker compose logs --tail 100 api`. Para parar sem apagar o banco persistido, use `docker compose down`; **não** use `docker compose down -v` se quiser manter os dados.

#### Conta inicial local

O Compose configura o seed apenas para desenvolvimento. A conta administrativa é `admin@ellosaude.com`; a senha é a definida em `DEVELOPMENT_SEED_PASSWORD` no `.env` local. Não existe senha padrão versionada. Não use o seed nem o ambiente `Development` em produção.

#### Portal do paciente e financeiro

A equipe clínica pode provisionar uma conta do portal na ficha do paciente. A senha temporária é exibida uma única vez; entregue-a ao paciente por um canal externo seguro. No primeiro login a troca de senha é obrigatória. O paciente só vê dados vinculados à sua própria conta e pode cancelar agendamentos com pelo menos 24 horas de antecedência.

O módulo financeiro é administrativo: registra valores, recebimentos, forma e status do pagamento para gestão da clínica. Não há gateway nem processamento de pagamentos no sistema.

Para executar ou acompanhar a suite de testes:

```powershell
dotnet test .\ElloSaude.sln
```

Consulte também o [guia operacional do Docker Desktop](../docs/DOCKER.md).

---

## 🔐 Configuração de desenvolvimento

Para executar a API fora do Docker, configure `ConnectionStrings:DefaultConnection`, `Jwt:Key` (no mínimo 32 bytes) e a senha de seed, se habilitada, por User Secrets ou variáveis de ambiente. Nunca grave credenciais ou chaves JWT em `appsettings.json` versionado.

---

## 🛡️ Validação de Segurança & LGPD
1. **Dados Isolados (SaaS Multi-tenant)**: O `TenantId` da clínica é injetado no token JWT e filtrado em nível de banco pelo EF Core. Nenhuma clínica consegue acessar dados de outra.
2. **Confidencialidade de Prontuário (RF10)**: A API restringe prontuários ao profissional autor; perfis de Secretaria e Admin não possuem acesso clínico.
3. **Criptografia de Senha**: Nenhuma senha é gravada em texto plano. Utiliza-se algoritmo BCrypt com Salt forte.
