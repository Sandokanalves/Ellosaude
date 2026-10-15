# 📝 CHANGELOG — ELLO SAÚDE

Registro cronológico de todas as alterações realizadas no projeto, seguindo o padrão [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/) e [Conventional Commits](https://www.conventionalcommits.org/).

---

## [Unreleased] — Etapa 8 em andamento

### Added
- Suite `MultiTenantIsolationTests` com 6 novos testes de integração cobrindo isolamento de dados entre tenants (receitas, agendamentos, portal do paciente).
- Documentação técnica completa em `docs/`:
  - `ARQUITETURA.md` — Camadas, multi-tenancy, padrão Outbox, fluxo de dados.
  - `SEGURANCA_E_LGPD.md` — Controles de segurança, RBAC, auditoria, variáveis de ambiente.
  - `REQUISITOS_FUNCIONAIS.md` — Matriz completa de requisitos com status de implementação.
  - `MODELO_DE_DADOS.md` — Entidades, campos, enums, relacionamentos, histórico de migrations.
  - `API_ENDPOINTS.md` — Inventário completo de endpoints REST e SignalR.
  - `TESTES.md` — Relatório de cobertura e instruções de execução.
  - `DESENVOLVIMENTO.md` — Guia de onboarding para novos desenvolvedores.
  - `DEPLOY_PRODUCAO.md` — Checklist, backup, rolling update, rollback, Nginx, disaster recovery.
  - `BANCO_DE_DADOS.md` — Migrations, backup, restauração, boas práticas.
  - `PACOTES_E_DEPENDENCIAS.md` — Inventário de dependências com licenças.
  - `OPERACAO_E_MONITORAMENTO.md` — Logs, diagnósticos, alertas, comandos de emergência.
  - `CHANGELOG.md` — Este arquivo.

---

## [0.7.0] — 2026-10-14 — Etapa 7: Docker e Sala de Espera

### Added
- Dockerfile multi-stage para a API (.NET 9).
- Dockerfile multi-stage para o frontend (Node 20 build + Nginx Alpine serve).
- `docker-compose.yml` unificado: `sqlserver`, `rabbitmq`, `api`, `frontend`.
- Volume persistente nomeado `sqlserver_data` para dados do banco.
- Healthchecks nos containers SQL Server, RabbitMQ, API e frontend.
- Arquivos `.dockerignore` e `.env.example`.
- `WaitingRoomHub` (SignalR) para notificações em tempo real da sala de espera.
- `OutboxPublisherService` (background worker) garante entrega confiável de mensagens ao RabbitMQ.
- `WaitingRoomNotificationConsumerService` consome mensagens e notifica via SignalR.
- Confirmação de consulta via link WhatsApp.
- Histórico imutável de transições de status de agendamentos (`AppointmentStatusHistory`).

### Commits
- `07c5623` — feat: commit fatia depois di registra historico (incompleto)
- `125fc3d` — feat(appointments): registra historico imutavel de status
- `81950d7` — feat(appointments): adiciona confirmacao via WhatsApp
- `3dfee03` — feat(waiting-room): adiciona notificacoes RabbitMQ confiaveis
- `954824a` — fix(messaging): publish RabbitMQ messages asynchronously
- `d5872f6` — feat(appointments): add real-time waiting room notifications
- `5fa99a5` — feat(appointments): add virtual waiting room lifecycle

---

## [0.5.0] — 2026-10-09 — Etapas 3–5: Backend Clínico Completo + Frontend

### Added
- Entidades de domínio completas: `Professional`, `DoctorAvailability`, `ScheduleBlock`, `Prescription`, `PrescriptionItem`, `PaymentRecord`, `AuditLog`, `OutboxMessage`, `ClinicalAttachment`, `ClinicalMarker`, `InAppNotification`.
- Campos adicionais em `Patient`: telefone, gênero, profissão, endereço, contato de emergência, origem de marketing, alergias, medicações contínuas, condições crônicas.
- Campos adicionais em `Appointment`: valor previsto, tipo de cobrança, observações, histórico de status.
- `MedicalRecordAddendum` para correções sem sobrescrita do prontuário original.
- Migrations: `AddCompleteSaasClinicalSchema` e `AddPatientPortalOwnershipAndAppointmentBoundRecords`.
- Constraint `CK_MedicalRecords_AppointmentOrLegacy` no banco.
- Módulo de agenda: configuração de disponibilidade semanal, horários livres, bloqueios.
- Módulo de prontuário: imutabilidade, adendos, auditoria de acesso.
- Módulo de receituário: criação, finalização, geração de PDF (QuestPDF).
- Módulo financeiro: pagamento, pagamento parcial, retorno gratuito, cancelamento, relatório.
- Portal do Paciente (API): agendamento, cancelamento, receitas, senha temporária.
- Frontend: `PatientDetailPage.vue` com 5 abas (Resumo Clínico, Histórico, Marcadores Anatômicos, Exames/Docs, Documentos Gerados).
- Frontend: `FinancialPage.vue` com filtros, tabela e modal de pagamento.
- Frontend: `PatientPortalPage.vue` com agendamentos e receitas.
- Frontend: `SettingsPage.vue` com dados da clínica, preços e gestão de equipe.
- Frontend: `AnatomicalMarkerCanvas.vue` para marcadores visuais.
- Frontend: Sidebar dinâmica adaptada por role.

### Fixed
- Correção do multi-tenancy em `Clinics` (aplicação do Global Query Filter).
- Profissional vinculado obrigatoriamente ao agendamento.

### Commits
- `e8334a5` — feat(backend): concluir modulos clinicos, agenda e financeiro
- `9c6bed1` — fix(appointments): enforce professional ownership
- `8f17b79` — fix(multitenancy): apply tenant filter to clinics

---

## [0.2.0] — 2026-05-04 — Fundação de Segurança

### Added
- `ValidationBehavior<TRequest, TResponse>`: pipeline MediatR com FluentValidation automático.
- `ExceptionMiddleware` com `ProblemDetails` (RFC 7807): 400, 403, 404, 409, 500.
- JWT com validação de `Issuer`, `Audience`, expiração e chave via variável de ambiente.
- CORS configurável por variável de ambiente.
- Health checks: `/health` e `/health/ready` com verificação do SQL Server.
- Testes unitários: `ValidationBehaviorTests`, `CreatePatientHandlerTests`.
- Testes de integração: `AuthAndPatientsIntegrationTests` (fluxo completo de login e CRUD de paciente).

### Fixed
- Chave JWT insegura substituída por variável de ambiente obrigatória.

### Commits
- `1cdebfc` — fix(security): correcao do middleware problem details, pipeline fluentvalidation, seguranca jwt e healthchecks
- `20260504172915_AddProfessionalIdToAppointment` — migration

---

## [0.1.0] — 2026-04-23 — Criação Inicial

### Added
- Estrutura inicial Clean Architecture: Domain, Application, Infrastructure, Api.
- Entidades base: `User`, `Patient`, `Appointment`, `MedicalRecord`, `Clinic`.
- Migration inicial (`InitialCreate`).
- Autenticação JWT básica.
- Controllers: Auth, Patients, Appointments, MedicalRecords, Clinics.
- Frontend Vue 3 + PrimeVue: Login, Lista de Pacientes, Agenda básica.
- Repositório Git configurado com monorepo (API + Web + Docs).
- `.gitignore` global unificado.
- Documentação: `AUDITORIA_TECNICA.md`, `PLANO_DE_IMPLEMENTACAO.md`.

### Commits
- `4e5eda4` — docs(audit): entrega da auditoria tecnica completa, plano de implementacao e setup do monorepo
