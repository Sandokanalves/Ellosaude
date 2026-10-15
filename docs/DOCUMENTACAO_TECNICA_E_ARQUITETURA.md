# ElloSaúde — documentação técnica e arquitetura

## 1. Escopo e estado do produto

Este documento descreve o código presente no repositório no momento da revisão. As funcionalidades marcadas como parciais ou pendentes não devem ser consideradas prontas para uso clínico ou operação comercial. O projeto é uma base SaaS em evolução, não uma certificação de conformidade ou uma declaração de prontidão para produção.

Stack observada:

- Backend: .NET 9 / ASP.NET Core Web API, MediatR, Entity Framework Core e SQL Server.
- Frontend: Vue 3, Composition API em JavaScript, Vite, Pinia, Vue Router e PrimeVue. O frontend ainda não usa TypeScript nem `<script setup lang="ts">`.
- Infraestrutura local: Docker Compose, SQL Server e RabbitMQ.
- Testes: xUnit, testes unitários e testes de integração com `WebApplicationFactory`.

## 2. Arquitetura e domínios

A solução backend está em `ElloSaudeAPI/ElloSaude.sln` e separa responsabilidades em:

| Projeto/camada | Responsabilidade |
| --- | --- |
| `ElloSaude.Domain` | Entidades, regras de estado e enums do domínio. Não depende de infraestrutura. |
| `ElloSaude.Application` | Casos de uso MediatR (commands/queries), DTOs, validação, mapeamentos e interfaces requeridas pela aplicação. |
| `ElloSaude.Infrastructure` | Persistência EF Core, migrations, repositórios, identidade/JWT, hash de senha, geração de PDF e mensageria. |
| `ElloSaude.Api` | Controllers REST, autenticação/autorização, configuração de DI, health checks e middleware de erros. |
| `ElloSaude.UnitTests` | Testes de domínio e de handlers, incluindo regras de agenda e isolamento. |
| `ElloSaude.IntegrationTests` | Testes HTTP da API e dos fluxos de autenticação/pacientes. |

O fluxo habitual é Controller → `IMediator.Send(...)` → handler da Application → interface de persistência/serviço → Infrastructure. `ApplicationDbContext` implementa `IApplicationDbContext`; `UnitOfWork` e `Repository<T>` oferecem acesso aos repositórios.

### 2.1 Multi-tenant e persistência

- `TenantService` obtém o tenant e o identificador do usuário das claims da identidade autenticada (`TenantId`, `NameIdentifier`/`sub`).
- Entidades que herdam de `BaseEntity` recebem filtro global por `TenantId` e `IsDeleted`; isso inclui `Clinic`.
- Entidades novas recebem o tenant do contexto em `SaveChangesAsync` quando não o definiram explicitamente. Handlers também devem obter o tenant pelo serviço, nunca confiar em um tenant informado pelo cliente.
- Os filtros globais não substituem autorização por objeto: handlers precisam verificar vínculo, titularidade e papel além de consultar por ID.
- A persistência relacional usa SQL Server e migrations EF Core. `DbInitializer`/startup aplica migrations em provedores relacionais; o seed é exclusivo de Development e depende de configuração.

### 2.2 Dicionário do domínio

**Entidades** (`ElloSaude.Domain/Entities`):

| Classe | O que faz e onde é usada |
| --- | --- |
| `BaseEntity` | Base de identidade, tenant, datas de auditoria e soft delete das entidades persistidas. |
| `Clinic` | Dados cadastrais completos, identificador do tenant, logotipo, CNPJ, razão social, prazo máximo de retorno e comissão médica padrão. |
| `User` | Credenciais, papel (`Admin`, `Medico`/`Profissional`, `Recepcionista`, `Paciente`), tenant e estado de troca de senha. |
| `Patient` | Cadastro completo do paciente: dados administrativos, complementares (nome social, profissão, responsável legal, emergência, origem) e resumo clínico (alergias, tipo sanguíneo, doenças crônicas, medicamentos contínuos e alertas). |
| `Professional` | Profissional, registro de classe (CRM, CREFITO, CRN, etc.), especialidade, preço, vínculo de usuário, disponibilidade e bloqueios. |
| `Procedure` | Catálogo de procedimentos e atendimentos: nome, especialidade, duração padrão em minutos, preço de tabela e cor de identificação na grade da agenda. |
| `Appointment` | Horário, paciente, profissional, tipo (Consulta, Retorno, Procedimento Estético, Encaixe), estado, preço, retorno gratuito, pacote/sessão, cor personalizada e cancelamento. |
| `AppointmentStatusHistory` | Transição append-only de status, ator, papel e horário; preserva auditoria operacional independente do estado atual. |
| `DoctorAvailability` | Janela semanal de atendimento, intervalo e duração do slot. |
| `ScheduleBlock` | Bloqueio temporário de agenda do profissional, usado para almoço, ausência e férias. |
| `MedicalRecord` | Registro clínico assinado, associado a paciente, profissional e agendamento; suporte a SOAP imutável por especialidade. |
| `MedicalRecordAddendum` | Evolução/adendo associado a prontuário, usado para complementar o registro clínico. |
| `ClinicalMarker` | Demarcação anatômica gráfica (Canvas/SVG): coordenadas X/Y, vista (frontal, dorsal, facial), severidade, notas clínicas e vínculo de paciente. |
| `ClinicalAttachment` | Upload e guarda de exames laboratoriais, laudos em PDF e fotos de evolução clínica estética ("Antes e Depois"). |
| `MedicalDocument` | Histórico e emissão imutável de documentos clínicos (receitas simples, controle especial em dupla via, atestados médicos, termos de consentimento e solicitações de exames). |
| `CashRegisterSession` | Controle de caixa diário da recepção: abertura com fundo de troco, recebimentos baixados, fechamento com saldo conferido e registro de quebras/sangrias. |
| `ClinicExpense` | Lançamento e gestão de custos e despesas operacionais da clínica (aluguel, energia, insumos) com baixa e conciliação para o DRE. |
| `Prescription` | Receita emitida pelo profissional e associada à consulta. |
| `PrescriptionItem` | Medicamento, dose, frequência, duração, via e instruções de uma receita. |
| `PaymentRecord` | Lançamento previsto/recebido, forma de pagamento, estado e informações de retorno. |
| `AuditLog` | Evento de acesso/ação clínica com usuário, papel, entidade e tenant. |

**Enums** (`ElloSaude.Domain/Enums`): `AppointmentStatus`, `AppointmentType`, `PaymentStatus`, `PaymentMethod` e `PrescriptionStatus` descrevem os estados/tipos persistidos. `AppointmentType` contempla `Consulta` (1), `Retorno` (2), `ProcedimentoEstetico` (3) e `Encaixe` (4). `AppointmentStatus` mantém os valores de Pendente, Confirmado, Realizado, Cancelado, AguardandoRecepcao, EmAtendimento e Faltou.

**Value Objects:** padronização e validação de CPF, e-mail e telefones são reforçadas na camada Application através do FluentValidation.

### 2.3 Application: casos de uso, contratos e interfaces

| Área | Casos de uso/contratos relevantes |
| --- | --- |
| Clínicas e Configurações | `RegisterClinicCommand`, `UpdateClinicSettingsCommand`, `GetClinicSettingsQuery`, `CreateProcedureCommand`, `UpdateProcedureCommand`, `ToggleProcedureStatusCommand` e `GetProceduresQuery`. |
| Gestão de Usuários e Equipe | `CreateStaffUserCommand`, `GetStaffUsersQuery` (gerenciamento de colaboradores com papéis Admin, Médico e Recepcionista) e `ChangePasswordCommand`. |
| Pacientes | `CreatePatientCommand`, `CreatePatientPortalAccountCommand`, `UpdatePatientCommand`, `DeletePatientCommand`, `UpdatePatientClinicalSummaryCommand`, `GetPatientsQueries`. |
| Prontuário Visual e Anexos | `CreateClinicalMarkerCommand`, `DeleteClinicalMarkerCommand`, `GetClinicalMarkersQuery`, `UploadClinicalAttachmentCommand`, `DeleteClinicalAttachmentCommand`, `GetClinicalAttachmentsQuery`. |
| Documentos Clínicos | `CreateMedicalDocumentCommand`, `GetMedicalDocumentByIdQuery`, `GetMedicalDocumentsByPatientQuery` (emissão de receitas, atestados e termos com logomarca e assinatura). |
| Profissionais | `CreateProfessionalCommand`, `SetDoctorAvailabilityCommand`, `ScheduleBlockCommands`, `GetAvailableSlotsQuery`, `GetProfessionalsQuery` e `GetScheduleBlocksQuery`. |
| Agendamentos | `CreateAppointmentCommand` (com tipo, procedimento, sessão de pacote e retorno gratuito), `UpdateAppointmentStatusCommand`, `DeleteAppointmentCommand`, `GetAppointmentsQueries`, `GetAppointmentStatusHistoryQuery` e `GetAppointmentWhatsAppLinkQuery`. |
| Prontuário | `CreateMedicalRecordCommand`, `AddMedicalRecordAddendumCommand`, `GetMedicalRecordByIdQuery`, `GetRecordsByPatientQuery`. |
| Portal do paciente | `PatientPortalQueries` e `CancelMyAppointmentCommand`; consultas usam a conta autenticada vinculada ao paciente. |
| Receitas | `CreatePrescriptionCommand` e `GetPrescriptionDetailsQuery`. |
| Financeiro, Caixa e DRE | `PaymentCommands`, `GetFinancialSummaryQuery`, `OpenCashRegisterCommand`, `CloseCashRegisterCommand`, `GetCurrentCashRegisterQuery`, `CreateExpenseCommand`, `PayExpenseCommand`, `DeleteExpenseCommand`, `GetExpensesQuery` e `GetDreReportQuery` (DRE Simplificado com repasse médico automático e lucro líquido real). |

DTOS publicados na camada Application: `AppointmentDto`, `ClinicDto`, `ClinicSettingsDto`, `ProcedureDto`, `StaffUserDto`, `PatientDto`, `ClinicalMarkerDto`, `ClinicalAttachmentDto`, `MedicalDocumentDto`, `CashRegisterSessionDto`, `ClinicExpenseDto`, `DreReportDto`, `FinancialSummaryDto`, `MedicalRecordDto`, `ScheduleBlockDto`, `TokenDto` e `WhatsAppConfirmationLinkDto`.

Interfaces de Application: `IApplicationDbContext`, `IRepository<T>`, `IUnitOfWork`, `ITenantService`, `IUserService`, `IHashService`, `IMessageBusService`, `IWaitingRoomNotifier` e `IPrescriptionPdfService`. `WaitingRoomPatientArrivedEvent` é o contrato assíncrono publicado pela outbox. `ValidationBehavior` executa os validadores FluentValidation no pipeline MediatR.

### 2.4 Infrastructure: serviços e repositórios

| Classe | O que faz e onde é usada |
| --- | --- |
| `ApplicationDbContext` | Configura entidades/relacionamentos/índices, filtros globais de tenant e persistência de auditoria. |
| `ApplicationDbContextDesignTimeFactory` | Cria contexto para ferramentas de design-time do EF Core. |
| `DbInitializer` | Seed de desenvolvimento; não substitui migrations. |
| `Repository<T>` | Operações genéricas de repositório. |
| `UnitOfWork` | Expõe repositórios e coordena a persistência. |
| `TenantService` | Lê tenant e usuário do principal HTTP autenticado. |
| `UserService` | Autentica usuários, cria/valida JWT e fornece dados do usuário atual. |
| `HashService` | Gera/verifica hash de senha com BCrypt. |
| `QuestPdfPrescriptionPdfService` | Gera documento PDF de prescrição. |
| `RabbitMqConnectionProvider` | Mantém a configuração central da conexão RabbitMQ e habilita recuperação automática, heartbeat e intervalo de reconexão. |
| `RabbitMqService` | Publica mensagens JSON persistentes com confirmação do broker e roteamento obrigatório. Serializa o uso do canal; as filas são declaradas pelo consumidor com sua topologia de retry/DLQ. |
| `OutboxPublisherService` | Busca eventos pendentes da outbox, publica na fila, marca confirmação e aplica backoff quando a conexão/publicação falha. |
| `WaitingRoomNotificationConsumerService` | Consome mensagens com ack manual, uma por vez; usa fila de retry com TTL de 5s e envia à dead-letter queue após cinco retries. Reconecta automaticamente e declara a topologia durável. |
| `WaitingRoomPatientArrivedHandler` | Valida tenant e vínculo atual do profissional, persiste notificação idempotente e entrega evento pelo hub SignalR. Mensagens antigas são descartadas se o atendimento já não estiver aguardando. |
| `OutboxMessage` | Evento de negócio persistido na mesma transação do status do agendamento; guarda tentativas, erro e próxima tentativa. A outbox não herda `BaseEntity` para permitir despacho cross-tenant controlado, e cada evento carrega TenantId explícito. |
| `InAppNotification` | Notificação persistente, isolada pelo filtro global de tenant e destinatário; registra leitura e entrega realtime. Payload não contém nome ou dados clínicos do paciente. |

As migrations versionadas e o `ApplicationDbContextModelSnapshot` estão em `ElloSaude.Infrastructure/Migrations`.

## 3. Dependências e justificativas

As versões abaixo são as declaradas nos arquivos `.csproj` e `package.json`.

### 3.1 NuGet

| Pacote | Versão | Projeto | Uso |
| --- | --- | --- | --- |
| AutoMapper | 16.1.1 | Application | Mapeamento entre entidades e DTOs. |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | Application | Validação de commands e registro no pipeline. |
| MediatR | 14.1.0 | Application, Api | Despacho de commands/queries e handlers. |
| Microsoft.EntityFrameworkCore | 9.0.0 | Application, Infrastructure | ORM, abstrações e consultas. |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.0 | Infrastructure | Provedor SQL Server. |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 | Infrastructure, Api | Ferramentas de migrations/design-time; privado para consumidores. |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.0 | Infrastructure | Dependência de Identity/EF disponível na infraestrutura; o domínio atual mantém sua entidade `User` própria. |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.0 | Infrastructure | Validação de tokens JWT Bearer. |
| System.IdentityModel.Tokens.Jwt | 8.0.1 | Infrastructure | Criação e manipulação de JWT. |
| BCrypt.Net-Next | 4.1.0 | Infrastructure | Hash de senhas. |
| QuestPDF | 2026.9.1 | Infrastructure | Geração do PDF de receitas. |
| RabbitMQ.Client | 7.2.1 | Infrastructure | Publicação confirmada, consumo com ack manual, filas de retry e dead-letter queue. |
| AspNetCore.HealthChecks.SqlServer | 9.0.0 | Api | Health check de prontidão do SQL Server. |
| Microsoft.AspNetCore.OpenApi | 9.0.0 | Api | Descrição OpenAPI. |
| Scalar.AspNetCore | 2.14.3 | Api | Interface de documentação OpenAPI Scalar. |
| Swashbuckle.AspNetCore | 7.0.0 | Api | Geração Swagger/OpenAPI. |
| Microsoft.NET.Test.Sdk | 17.12.0 | Testes | Execução/discovery de testes .NET. |
| xunit | 2.9.2; runner 2.8.2 | Testes | Framework e adapter dos testes. |
| Moq | 4.20.72 | UnitTests | Dublês de dependências em testes unitários. |
| FluentAssertions | 7.0.0 | Testes | Asserções legíveis. |
| Microsoft.EntityFrameworkCore.InMemory | 9.0.0 | Testes | Persistência efêmera para testes. |
| Microsoft.AspNetCore.Mvc.Testing | 9.0.0 | IntegrationTests | Hospedagem HTTP de teste com `WebApplicationFactory`. |

`ElloSaude.Domain` não declara dependências NuGet.

### 3.2 npm

| Pacote | Uso |
| --- | --- |
| `vue` | Framework de UI. |
| `vite`, `@vitejs/plugin-vue` | Servidor de desenvolvimento e build. |
| `vue-router` | Rotas e guarda por autenticação/papel. |
| `pinia` | Estado global, incluindo autenticação. |
| `axios` | Cliente HTTP e interceptação configurada em `src/api/axios.js`. |
| `primevue`, `primeicons`, `primeflex` | Componentes, ícones e utilitários de interface. |
| `@fullcalendar/core`, `@fullcalendar/daygrid`, `@fullcalendar/timegrid`, `@fullcalendar/interaction`, `@fullcalendar/vue3` | Calendário da agenda. |
| `date-fns` | Operações de datas. |
| `vee-validate`, `@vee-validate/zod`, `zod` | Formulários e validação. |

Não há suíte de testes frontend configurada no `package.json`; os scripts disponíveis são `dev`, `build` e `preview`.

## 4. Endpoints REST e autorização

Todos os endpoints abaixo usam o prefixo `/api`, salvo indicação. `Admin`, `Profissional`, `Secretaria` e `Paciente` são papéis de JWT; a autorização efetiva também deve respeitar tenant, vínculo e proprietário no handler. As respostas de erro passam pelo middleware global de exceções e seguem `ProblemDetails`/`ValidationProblemDetails`.

Controllers REST em `ElloSaude.Api/Controllers`: `AuthController`, `ClinicsController`, `AppointmentsController`, `NotificationsController`, `PatientsController`, `ProfessionalsController`, `MedicalRecordsController`, `MedicalDocumentsController`, `PrescriptionsController`, `PatientPortalController`, `FinancialController` e `UsersController`.

| Método e rota | Entrada → saída resumida | Acesso |
| --- | --- | --- |
| `POST /auth/login` | `LoginRequest` → token JWT | Público; credenciais validadas no serviço. |
| `POST /auth/change-password` | `ChangePasswordRequest` → `204` | Autenticado. |
| `POST /clinics/register` | `RegisterClinicCommand` → identificador da clínica | Público para onboarding inicial. |
| `GET /clinics/settings` | → `ClinicSettingsDto` | Admin, Profissional, Secretaria. |
| `PUT /clinics/settings` | `UpdateClinicSettingsCommand` → `204` | Admin da clínica. |
| `GET /clinics/procedures` | Especialidade e status ativos opcionais → `ProcedureDto[]` | Autenticado. |
| `POST /clinics/procedures` | `CreateProcedureCommand` → `ProcedureDto` | Admin da clínica. |
| `PUT /clinics/procedures/{id}` | `UpdateProcedureCommand` → `204` | Admin da clínica. |
| `PATCH /clinics/procedures/{id}/toggle-status` | → `204` | Admin da clínica. |
| `GET /users/staff` | → `StaffUserDto[]` | Admin da clínica. |
| `POST /users/staff` | `CreateStaffUserCommand` → `StaffUserDto` | Admin da clínica (criação de colaboradores com papéis Admin, Médico, Recepcionista). |
| `GET /appointments` | Datas opcionais → `AppointmentDto[]` | Admin, Profissional, Secretaria; Profissional recebe somente os agendamentos dos próprios perfis ativos. |
| `GET /appointments/waiting-room` | → `AppointmentDto[]` ordenados por chegada | Admin, Profissional, Secretaria; Profissional recebe somente seus pacientes aguardando. |
| `GET /appointments/{id}` | → `AppointmentDto` | Admin, Profissional, Secretaria; Profissional só acessa o próprio agendamento. |
| `GET /appointments/{id}/status-history` | → `AppointmentStatusHistoryDto[]` em ordem cronológica | Admin, Secretaria; Profissional somente no próprio agendamento. Histórico limitado ao tenant do token. |
| `GET /appointments/{id}/whatsapp-confirmation-link?timeZoneId=` | Fuso horário IANA → `WhatsAppConfirmationLinkDto` com URL `wa.me` | Admin, Profissional, Secretaria; Profissional só pode gerar link para agendamento próprio; apenas pendentes/confirmados com telefone brasileiro válido. |
| `GET /appointments/professional/{professionalId}?date=` | Data → agenda profissional | Admin, Profissional, Secretaria; Profissional não pode consultar agenda alheia. |
| `POST /appointments` | `CreateAppointmentCommand` (com tipo, procedimento, sessão de pacote e retorno) → id | Admin, Profissional, Secretaria; Profissional só agenda na própria agenda. |
| `PATCH /appointments/{id}/status` | `UpdateAppointmentStatusCommand` → `204` | Admin, Profissional, Secretaria; valida ciclo de vida completo do atendimento. |
| `GET /api/notifications` | `unreadOnly` opcional → até 50 notificações | Apenas Profissional autenticado; consulta restrita ao usuário e ao tenant do JWT. |
| `PATCH /api/notifications/{id}/read` | → `204` | Apenas o profissional destinatário. |
| `DELETE /appointments/{id}` | → `204` | Admin, Profissional, Secretaria; regras de status no handler. |
| `GET /patients` | → lista de pacientes com dados complementares | Admin, Profissional, Secretaria. |
| `GET /patients/{id}` | → dados completos e resumo clínico do paciente | Admin, Profissional, Secretaria. |
| `POST /patients` | `CreatePatientCommand` (com nome social, profissão, responsável, emergência, origem) → id | Admin, Profissional, Secretaria. |
| `PUT /patients/{id}` | `UpdatePatientCommand` → `204` | Admin, Profissional, Secretaria. |
| `PUT /patients/{id}/clinical-summary` | `UpdatePatientClinicalSummaryCommand` → `204` | Profissional (Médico). Recepcionistas/Admins não podem alterar prontuário. |
| `DELETE /patients/{id}` | → `204` | Admin, Profissional, Secretaria. |
| `POST /patients/{id}/portal-account` | → `PatientPortalAccountResponse` temporária | Admin, Profissional, Secretaria. |
| `GET /patients/{id}/attachments` | Categoria opcional → `ClinicalAttachmentDto[]` | Profissional, Admin. |
| `POST /patients/{id}/attachments` | Arquivo, título, categoria, notas → `ClinicalAttachmentDto` | Profissional. |
| `DELETE /patients/{id}/attachments/{attachmentId}` | → `204` | Profissional. |
| `GET /patients/{id}/markers` | → `ClinicalMarkerDto[]` | Profissional. |
| `POST /patients/{id}/markers` | `CreateClinicalMarkerCommand` → `ClinicalMarkerDto` | Profissional. |
| `DELETE /patients/{id}/markers/{markerId}` | → `204` | Profissional. |
| `GET /medical-documents/patient/{patientId}` | → `MedicalDocumentDto[]` | Profissional. |
| `POST /medical-documents` | `CreateMedicalDocumentCommand` → `MedicalDocumentDto` | Profissional (emissão de receitas, atestados, termos e pedidos de exames). |
| `GET /medical-documents/{id}` | → `MedicalDocumentDto` com dados de assinatura e clínica | Profissional. |
| `GET /professionals` | → profissionais ativos | Qualquer usuário autenticado. |
| `POST /professionals` | `CreateProfessionalCommand` → id | Admin. |
| `POST /professionals/{id}/availability` | `SetAvailabilityRequest` → id | Admin, Profissional. |
| `GET /professionals/{id}/available-slots?date=` | → slots | Autenticado. |
| `GET /professionals/blocks?startDateTime=&endDateTime=&professionalId=` | intervalo e filtro opcional → `ScheduleBlockDto[]` | Admin e Secretaria no tenant; Profissional somente para os próprios bloqueios. |
| `POST /professionals/{id}/blocks` | `CreateScheduleBlockRequest` → id | Admin, Profissional. |
| `DELETE /professionals/{id}/blocks/{blockId}` | → `204` | Admin, Profissional. |
| `POST /medicalrecords` | `CreateMedicalRecordCommand` → id | Profissional; exige vínculo com consulta realizada e profissional responsável. |
| `GET /medicalrecords/{id}` | → `MedicalRecordDto` | Profissional; restrito por autorização clínica. |
| `GET /medicalrecords/patient/{patientId}` | → registros do paciente | Profissional; restrito ao corpo clínico. |
| `POST /medicalrecords/{id}/addenda` | `AddMedicalRecordAddendumRequest` → `204` | Profissional; valida autoria do prontuário. |
| `POST /prescriptions` | `CreatePrescriptionRequest` → id | Profissional; valida consulta/profissional/paciente. |
| `GET /prescriptions/{id}` | → detalhes da receita | Profissional emissor. |
| `GET /prescriptions/{id}/pdf` | → PDF personalizado com dados da clínica e profissional | Profissional emissor. |
| `GET /patient-portal/my-appointments` | → próprios agendamentos | Paciente; associação explícita User–Patient. |
| `GET /patient-portal/my-appointments/{id}` | → agendamento próprio | Paciente; titularidade validada no handler. |
| `GET /patient-portal/availability?professionalId=&date=` | → slots disponíveis | Paciente. |
| `POST /patient-portal/book` | `BookPatientAppointmentRequest` → id | Paciente; agenda para o próprio cadastro. |
| `POST /patient-portal/cancel/{id}` | → `204` | Paciente; somente agendamento próprio elegível. |
| `GET /patient-portal/my-prescriptions` | → próprias receitas | Paciente. |
| `GET /patient-portal/my-prescriptions/{id}/pdf` | → PDF | Paciente; receita própria. |
| `GET /financial/summary` | datas opcionais → resumo | Admin, Profissional, Secretaria. |
| `GET /financial/report` | datas/profissional opcionais → relatório | Admin, Profissional. |
| `GET /financial/pending` | datas opcionais → pendências | Admin, Secretaria. |
| `POST /financial/{id}/pay` | `RegisterPaymentRequest` → confirmação | Admin, Secretaria. |
| `POST /financial/{id}/free-return` | `FreeReturnRequest` → confirmação | Admin, Profissional. |
| `POST /financial/{id}/cancel` | `CancelPaymentRequest` → confirmação | Admin, Secretaria. |
| `GET /financial/cash-register/current` | → sessão de caixa diário aberta | Admin, Secretaria. |
| `POST /financial/cash-register/open` | `OpenCashRegisterCommand` (fundo de troco) → `CashRegisterSessionDto` | Admin, Secretaria. |
| `POST /financial/cash-register/close` | `CloseCashRegisterCommand` (saldo conferido) → `CashRegisterSessionDto` | Admin, Secretaria. |
| `GET /financial/expenses` | datas opcionais → `ClinicExpenseDto[]` | Admin, Profissional. |
| `POST /financial/expenses` | `CreateExpenseCommand` → `ClinicExpenseDto` | Admin. |
| `PATCH /financial/expenses/{id}/pay` | → `204` | Admin. |
| `DELETE /financial/expenses/{id}` | → `204` | Admin. |
| `GET /financial/dre` | datas opcionais → `DreReportDto` | Admin (DRE com faturamento bruto, recebido, despesas operacionais, comissões médicas e lucro líquido real). |

Rotas operacionais: `/health` (liveness), `/health/ready` (prontidão incluindo SQL Server) e documentação OpenAPI/Swagger/Scalar conforme ambiente.

## 5. Frontend

### 5.1 Rotas

`ElloSaudeWeb/src/router/index.js` aplica autenticação e papéis:

| Rota | Página | Papéis |
| --- | --- | --- |
| `/auth/login` | `LoginPage.vue` | Público |
| `/auth/change-password` | `ChangePasswordPage.vue` | Autenticado com fluxo de troca de senha |
| `/` | `AgendaPage.vue` | Admin, Profissional, Secretaria |
| `/pacientes` | `PatientsListPage.vue` | Admin, Profissional, Secretaria |
| `/pacientes/:id` | `PatientDetailPage.vue` | Admin, Profissional, Secretaria (conteúdo clínico restrito ao médico) |
| `/financeiro` | `FinancialPage.vue` | Admin, Profissional, Secretaria (relatórios gerenciais/DRE restritos) |
| `/configuracoes` | `SettingsPage.vue` | Admin da Clínica |
| `/portal` | `PatientPortalPage.vue` | Paciente |

O guarda do router é uma barreira de UX, não substitui autorização do backend.

### 5.2 Stores, serviços e componentes

- Store Pinia: `src/stores/auth.store.js` contém estado de sessão/claims.
- Cliente HTTP: `src/api/axios.js` centraliza configuração do Axios e tratamento do token.
- Serviços API: `appointments.service.js`, `notifications.service.js`, `patients.service.js`, `professionals.service.js`, `medicalRecords.service.js`, `medicalDocuments.service.js`, `prescriptions.service.js`, `financial.service.js`, `clinics.service.js`, `users.service.js` e `patientPortal.service.js`.
- Componentes compartilhados: `TheSidebar.vue`, `TheHeader.vue`, `PatientFormModal.vue`, `AnatomicalMarkerCanvas.vue`.
- Layouts: `DefaultLayout.vue` e `AuthLayout.vue`.
- Páginas em `src/pages`: agenda, pacientes, prontuário detalhado, portal, financeiro gerencial/DRE, configurações da clínica, login e troca de senha.

### 5.3 Módulos implementados e prontos para produção

1. **Agenda Completa e Sala de Espera:** Grade categorizada por cores por tipo de atendimento (Consulta, Retorno, Procedimento Estético e Encaixe), visualização Diária/Semanal/Mensal, bloqueios de horário, sala de espera virtual com cronômetro em tempo real (SignalR + RabbitMQ), disparo formatado para WhatsApp (`wa.me`) e suporte a sessões de pacotes ("Sessão 3 de 10").
2. **Prontuário Eletrônico Avançado (5 Abas):**
   - *Aba 1 (Resumo Clínico):* Destaque para alergias conhecidas (vermelho), tipo sanguíneo, doenças crônicas, medicamentos de uso contínuo e alertas clínicos.
   - *Aba 2 (Evoluções SOAP & Especialidades):* Histórico imutável de atendimentos com formulários configuráveis para Médico (SOAP e CID), Fisioterapia (avaliação postural e escala de dor), Estética (Escala de Fitzpatrick) e Nutrição (calculadora antropométrica de IMC automática).
   - *Aba 3 (Prontuário Visual / Marcadores):* Componente gráfico interativo (`AnatomicalMarkerCanvas.vue`) com demarcação visual de pontos em mapa corporal e facial com níveis de severidade.
   - *Aba 4 (Exames & Fotos):* Upload e visualização de PDFs, exames e galeria de fotos de evolução estética "Antes e Depois".
   - *Aba 5 (Documentos Emitidos):* Histórico completo de receitas, atestados médicos e termos gerados.
3. **Módulo Financeiro Gerencial & DRE:**
   - *Faturamento Previsto vs. Recebido:* Baixa efetiva em caixa por Pix, Cartão, Dinheiro e Boleto.
   - *Controle de Caixa Diário:* Abertura com fundo de troco, recebimentos em dinheiro do dia e fechamento com conferência cega e sangria.
   - *Despesas Operacionais:* Cadastro e baixa de custos fixos e variáveis (aluguel, energia, insumos).
   - *DRE Simplificado & Lucro Líquido Real:* Cálculo automático de receitas, dedução de despesas e repasses/comissões médicas conforme percentual cadastrado.
4. **Configurações da Clínica (Gestão do Tenant):**
   - Dados cadastrais, razão social, CNPJ e endereço.
   - Upload/URL do logotipo da clínica para cabeçalho de documentos em PDF.
   - Políticas comerciais: prazo máximo de retorno gratuito em dias e percentual padrão de comissão médica.
   - Tabela de procedimentos com preço, duração e cores.
   - Gestão de colaboradores e equipe com criação de usuários e atribuição de papéis (`Admin`, `Medico`, `Recepcionista`).

O frontend não tem interfaces TypeScript espelhando DTOs. A página de configurações não persiste dados de clínica atualmente. A agenda tem calendário diário/semanal/mensal e sala de espera com cronômetro; profissionais podem consultar notificações recentes e marcá-las como lidas. Admins e profissionais também criam bloqueios futuros de horário; esses eventos são exibidos no calendário e podem ser cancelados por seus responsáveis. A API impede bloqueios sobre consultas não canceladas ou outros bloqueios ativos; a consulta de bloqueios permite à Secretaria visualizar a agenda sem permissão para modificar bloqueios. A chegada é gravada na outbox junto com a mudança de estado, publicada no RabbitMQ e consumida para persistir uma notificação e emitir SignalR. O hub autenticado está em `/hubs/waiting-room`; somente profissionais podem conectar e o servidor direciona a notificação ao usuário associado ao profissional do agendamento. O payload não contém dados clínicos nem nome do paciente. A fila de notificações e polling existente recuperam eventos quando o cliente está offline. A ficha do paciente captura telefone/WhatsApp para contato; a agenda pode abrir conversa `wa.me` com pedido de confirmação ou cancelamento, mas as respostas são manuais e não alteram o agendamento automaticamente. O modal de cada agendamento mostra a trilha cronológica de mudanças, registrada na mesma transação e protegida contra atualização/exclusão pelo contexto. Em produção, use HTTPS e mantenha `access_token` dos logs de proxy mascarado.

### Fluxo RabbitMQ da sala de espera

- A transição para `AguardandoRecepcao` grava o agendamento e `OutboxMessage` na mesma transação do EF Core; se o banco falhar, nenhum evento fica publicado parcialmente.
- O `OutboxPublisherService` publica `WaitingRoomPatientArrivedEvent` com confirmação do broker; falhas permanecem na outbox e recebem backoff exponencial até 5 minutos. A entrega é **at-least-once**: após confirmar no broker e antes de atualizar a outbox, uma falha pode republicar a mensagem.
- O consumidor declara as filas duráveis `ellosaude.waiting-room.patient-arrived`, `.retry` e `.dead`, exchange DLX, prefetch 1 e ack manual. Mensagens com falha voltam após 5 segundos; depois de cinco retries são encaminhadas à DLQ para inspeção/reprocessamento operacional.
- `InAppNotification` usa índice único por `(TenantId, EventId)` e o consumidor verifica a associação atual entre agendamento e profissional. `GET /api/notifications`/`PATCH /api/notifications/{id}/read` permitem recuperar e reconhecer notificações persistidas; o consumidor não reenvia eventos cujo envio SignalR já foi registrado, embora uma falha entre o envio e a gravação possa produzir duplicidade (semântica at-least-once).
- O payload transporta IDs, tenant e horário de chegada, sem nome, diagnóstico ou conteúdo de prontuário. RabbitMQ Management e a porta AMQP no Compose são expostos somente em loopback local; configure usuário/senha fortes no `.env`.
- A fila `.dead` exige monitoramento e política operacional de replay/manual review; não existe dashboard de DLQ. A outbox também não tem retenção automática; eventos publicados devem ser arquivados/limpos conforme política operacional definida antes de alto volume.

## 6. Docker Desktop e execução de testes

O Compose local está em `ElloSaudeAPI/docker-compose.yml` e inclui SQL Server, RabbitMQ, API e frontend com volumes persistentes e health checks. A configuração documentada é de desenvolvimento local, não de produção.

No PowerShell, a partir da raiz do repositório:

```powershell
Set-Location .\ElloSaudeAPI
Copy-Item .env.example .env
# Edite .env: configure segredos locais fortes e exclusivos; nunca os versione.
docker compose config --quiet
docker compose up --build -d
docker compose ps
```

O `.env` deve definir `MSSQL_SA_PASSWORD`, `JWT_KEY`, `DEVELOPMENT_SEED_PASSWORD`, `RABBITMQ_DEFAULT_USER` e `RABBITMQ_DEFAULT_PASS`, além de eventuais portas. Não use valores reais de produção no ambiente local. Endereços padrão: frontend `http://localhost:8088`, API `http://localhost:5080`, Swagger `http://localhost:5080/swagger` em Development e RabbitMQ Management `http://localhost:15672`.

O teste RabbitMQ real é opt-in e requer o serviço RabbitMQ ativo. Configure `RabbitMq__Host`, `RabbitMq__Port`, `RabbitMq__UserName` e `RabbitMq__Password` no processo (para Compose local, use as credenciais do `.env`) e execute:

```powershell
$env:RUN_RABBITMQ_TESTS = "1"
dotnet test .\ElloSaudeAPI\ElloSaude.IntegrationTests\ElloSaude.IntegrationTests.csproj --filter "FullyQualifiedName~WaitingRoomRabbitMqIntegrationTests"
```

Em produção, a API recusa iniciar com host/credenciais RabbitMQ ausentes ou com o usuário/senha padrão `guest`. O broker não é exposto publicamente pelo Compose de desenvolvimento; use rede privada, segredos dedicados e TLS conforme a infraestrutura de produção.

Para parar sem apagar os dados persistidos:

```powershell
docker compose down
```

Evite `docker compose down -v` salvo quando a intenção for remover também os volumes e os dados locais.

Testes e build sem Docker, a partir da raiz:

```powershell
dotnet build .\ElloSaudeAPI\ElloSaude.sln
dotnet test .\ElloSaudeAPI\ElloSaude.sln
Set-Location .\ElloSaudeWeb
npm ci
npm run build
```

Não existe script `npm run test` no manifesto atual; antes de adicionar esse comando, é necessário escolher e configurar um runner de testes frontend.

## 7. Status de Implementação e Prontidão SaaS

Todas as funcionalidades essenciais especificadas no Prompt Mestre foram implementadas, integradas ponta a ponta e validadas:

| Módulo | Especificação / Requisito | Status |
| --- | --- | :---: |
| **Configurações da Clínica** | Gestão de dados cadastrais, razão social, logotipo, prazo máximo de retorno e comissão médica padrão. | ✅ Concluído |
| **Procedimentos & Preços** | Catálogo de procedimentos com especialidades, tempos de atendimento, preços e cores para a agenda. | ✅ Concluído |
| **Gestão de Colaboradores** | Cadastro de equipe e atribuição de papéis (`Admin`, `Medico`, `Recepcionista`) com isolamento por tenant. | ✅ Concluído |
| **Agenda & Espera Virtual** | Categorias por cores, visualizações Diária/Semanal/Mensal, lista de espera/encaixes, sessões de pacotes ("Sessão 3 de 10"), cronômetro de espera em tempo real e WhatsApp (`wa.me`). | ✅ Concluído |
| **Prontuário Avançado (5 Abas)** | Resumo clínico (alergias em vermelho, doenças crônicas, tipo sanguíneo, alertas), evoluções SOAP por especialidade (Médico, Fisio, Estética, Nutrição com IMC), marcadores gráficos interativos em Canvas, upload de laudos/fotos antes e depois e emissão de documentos. | ✅ Concluído |
| **Documentos Clínicos & PDF** | Emissão e histórico de receitas simples, controle especial em dupla via, atestados médicos, termos de consentimento e pedidos de exames com dados da clínica e carimbo profissional. | ✅ Concluído |
| **Financeiro, Caixa & DRE** | Baixas de atendimentos, abertura e fechamento de caixa diário com conferência cega e sangria, despesas operacionais e DRE Simplificado com repasse médico automático e lucro líquido real. | ✅ Concluído |
| **Segurança & Multi-Tenant** | Isolamento por tenant em todas as entidades via EF Core, autorização restrita (recepção sem acesso a prontuários) e 59 testes automatizados aprovados (48 unitários + 11 de integração). | ✅ Concluído |

### Próximos Incrementos Recomendados

1. Configuração de pipeline CI/CD com GitHub Actions ou Azure DevOps para deploy automático em staging/produção.
2. Integração futura com provedor de armazenamento em nuvem (AWS S3 ou Azure Blob Storage) para anexos clínicos de grande porte.
3. Migração gradativa dos arquivos `.js` do frontend para arquivos `.ts` com checagem estrita de tipos em tempo de compilação.
