# 🗺️ PLANO DE IMPLEMENTAÇÃO E EVOLUÇÃO — SAAS ELLO SAÚDE

**Objetivo:** Transformar o projeto ElloSaúde existente em uma solução SaaS multi-tenant comercializável para clínicas e consultórios, segura (LGPD), testada e pronta para produção no Docker Desktop.

---

## 1. PRINCÍPIOS E DIRETRIZES DO PLANO

1. **Preservação Arquitetural:** Manter a Clean Architecture (.NET 9 + C#), o padrão CQRS (MediatR), o EF Core com SQL Server e o frontend em Vue 3 (PrimeVue).
2. **Priorização Rígida:**
   - **Prioridade 1:** Segurança, isolamento multi-tenant, validação de integridade e fundação técnica.
   - **Prioridade 2:** Implementação dos módulos de negócio reais faltantes (Receituário/PDF, Financeiro real com formas de pagamento e consultas de retorno, Portal do Paciente, Disponibilidade de Agenda).
   - **Prioridade 3:** Prontuário médico com integridade e auditoria (LGPD/CFM).
   - **Prioridade 4:** Frontend profissional integrado, responsivo e com experiência SaaS moderna.
   - **Prioridade 5:** Bateria de testes automatizados (unitários, integração e isolamento multi-tenant).
   - **Prioridade 6:** Orquestração Docker completa (Front + API + DB + Volumes persistentes + Healthchecks).
   - **Prioridade 7:** Documentação exaustiva e preparação para produção.
3. **Não Destrutividade:** Não apagar migrações existentes indiscriminadamente; criar migrações incrementais organizadas.
4. **Governança Git Rígida:** Nenhum código avançará para a próxima etapa sem testes passando, commit semântico estruturado e sincronização via push com o repositório do GitHub.

---

## 2. ESTRATÉGIA DE VERSIONAMENTO GIT, COMMITS E SINCRONIZAÇÃO GITHUB

### 2.1. Estrutura do Repositório Monorepo
O projeto engloba o backend (`ElloSaudeAPI`), o frontend (`ElloSaudeWeb`), a documentação (`docs/`) e os artefatos de infraestrutura Docker em uma única raiz (`ClinicaProjeto`).
- **Raiz do Git:** Inicializada na raiz `ClinicaProjeto`.
- **Eliminação de `.git` aninhado:** Remover qualquer pasta `.git` interna em `ElloSaudeWeb` para evitar problemas de submodule indesejado.
- **Arquivo `.gitignore` Global Unificado:** Ignorar binários compilados (`bin/`, `obj/`), dependências de Node (`node_modules/`, `dist/`), cache do Visual Studio (`.vs/`), logs e arquivos de segredos locais (`.env`).
- **Branch Principal:** `main` (ou branch de desenvolvimento `develop` acordada).

### 2.2. Padrão de Mensagens de Commit (Conventional Commits)
Cada commit seguirá o padrão internacional da indústria:
- `docs: <descrição>` — Para atualizações de relatórios e documentação técnica.
- `fix: <descrição>` — Para correções de bugs, segurança ou tratamento de exceções.
- `feat: <descrição>` — Para inclusão de novas entidades, regras de negócio ou telas.
- `test: <descrição>` — Para novos testes unitários ou de integração.
- `refactor: <descrição>` — Para melhorias de design de código sem alteração funcional.
- `chore: <descrição>` — Para configurações de build, Docker, dependências ou scripts.

### 2.3. Protocolo Obrigatório de Conclusão de Cada Etapa
Ao finalizar cada etapa do plano, o seguinte fluxo deve ser executado obrigatoriamente:
1. **Validação de Integridade:** Rodar `dotnet test` e `npm run build` para garantir zero quebras.
2. **Revisão de Arquivos Modificados:** `git status` e verificação de arquivos sensíveis (garantir que nenhum `.env` com senhas reais seja comitado).
3. **Stage:** `git add .`
4. **Commit Semântico:** `git commit -m "<tipo>(<escopo>): <descrição detalhada das entregas da etapa>"`
5. **Push para o GitHub:** `git push origin <branch>`
6. **Registro no Relatório da Etapa:** Incluir o hash do commit e status do push no relatório da etapa.

---

## 3. CRONOGRAMA DE ETAPAS E SPRINT BACKLOG

```mermaid
graph TD
    A[Etapa 1: Auditoria Técnica, Planejamento e Setup Git] --> B[Etapa 2: Correção da Fundação e Segurança + Commit & Push]
    B --> C[Etapa 3: Domínio, Migrations e Multi-Tenancy + Commit & Push]
    C --> D[Etapa 4: Módulos de Negócio Críticos Backend + Commit & Push]
    D --> E[Etapa 5: Frontend Profissional e Portal do Paciente + Commit & Push]
    E --> F[Etapa 6: Suíte de Testes Automatizados + Commit & Push]
    F --> G[Etapa 7: Dockerização Completa e Persistência + Commit & Push]
    G --> H[Etapa 8: Documentação Completa, Auditoria Final e Release + Commit & Push]
```

---

## 4. DETALHAMENTO DAS ETAPAS COM COMMITS E PUSH

### ETAPA 1 — AUDITORIA TÉCNICA, PLANEJAMENTO E SETUP DO GIT
**Objetivo:** Auditar 100% do código existente, documentar o estado atual e estruturar o repositório Git unificado.

- [x] **1.1. Auditoria Completa:** Inspecionar backend, frontend, persistência, docker, segurança e testes.
- [x] **1.2. Documento de Auditoria Técnica:** Criação do arquivo `docs/AUDITORIA_TECNICA.md`.
- [x] **1.3. Documento do Plano de Implementação:** Criação e enriquecimento do `docs/PLANO_DE_IMPLEMENTACAO.md`.
- [x] **1.4. Setup do Repositório Git e GitHub:**
  - `.gitignore` global unificado na raiz do projeto.
  - Não há `.git` aninhado em `ElloSaudeWeb`.
  - Repositório na raiz, branch principal `main`, remote `origin` configurado.
- [x] **1.5. Commit e Push da Etapa 1:**
  - `git add .`
  - `git commit -m "docs(audit): entrega da auditoria tecnica completa, plano de implementacao e setup do monorepo"`
  - `git push -u origin main`
  - Evidência no histórico: `4e5eda4`; branch `main` sincronizada com `origin/main`.

---

### ETAPA 2 — CORREÇÃO DA FUNDAÇÃO TÉCNICA E SEGURANÇA
**Objetivo:** Eliminar vulnerabilidades críticas, padronizar respostas e garantir que regras de validação sejam efetivamente executadas.

- [x] **2.1. Pipeline de Validação Automática (FluentValidation):**
  - Implementar `ValidationBehavior<TRequest, TResponse>` como pipeline do MediatR.
  - Registrar todos os validadores da camada `Application`.
  - Retornar erros de validação padronizados automaticamente antes da execução dos handlers.
- [x] **2.2. Tratamento Global de Erros com Problem Details (RFC 7807):**
  - Refatorar `ExceptionMiddleware` para retornar `ValidationProblemDetails` em erros 400.
  - Tratar `KeyNotFoundException` (404), `UnauthorizedAccessException` (403), `InvalidOperationException` (409 Conflito) e exceções não tratadas (500 com mensagem segura e log interno).
- [x] **2.3. Blindagem de JWT e CORS:**
  - Configurar chave JWT segura via variáveis de ambiente com fallback restrito a ambiente de desenvolvimento.
  - Validar `Issuer`, `Audience` e tempo de expiração do token.
  - Configurar CORS com origens configuráveis, exigidas em produção.
- [x] **2.4. Health Checks e Observabilidade:**
  - Adicionar `builder.Services.AddHealthChecks()` com verificação do SQL Server.
  - Mapear endpoint `/health` e `/health/ready`.
- [x] **2.5. Commit e Push da Etapa 2:**
  - Testes: executar `dotnet test ElloSaude.sln`.
  - `git add .`
  - `git commit -m "fix(security): correcao do middleware problem details, pipeline fluentvalidation, seguranca jwt e healthchecks"`
  - `git push origin main`
  - Commit original: `1cdebfc`; hardening adicional desta continuação será registrado em novo commit.

---

### ETAPA 3 — MODELAGEM DE DOMÍNIO, MULTI-TENANCY E PERSISTÊNCIA
**Objetivo:** Estruturar as entidades que estão ausentes ou incompletas e versionar o esquema do banco de dados.

- [ ] **3.1. Evolução das Entidades Existentes:**
  - `BaseEntity`: Adicionar `CreatedAt`, `UpdatedAt`, `IsDeleted`.
  - `Patient`: Adicionar telefone, endereço, contato de emergência, sexo, status ativo/inativo.
  - `Appointment`: Adicionar valor previsto, tipo de cobrança (Normal vs Retorno Gratuito), observações, histórico de status e motivo de cancelamento.
  - `MedicalRecord`: Transformar em registro com integridade imutável; adicionar entidade filha `MedicalRecordAddendum` para retificações e anotações posteriores sem sobrescrita.
  - Campos/timestamps e adendos foram implementados; histórico de transições de status do agendamento ainda falta.
- [x] **3.2. Novas Entidades de Domínio:**
  - `Professional`: CRM, estado do conselho, especialidade, telefone, vínculo com clínica/usuário.
  - `DoctorAvailability` & `ScheduleBlock`: Dias da semana de atendimento, horário de início/fim, intervalo para almoço, duração padrão da consulta e bloqueios temporários (férias, congressos).
  - `Prescription` & `PrescriptionItem`: Paciente, profissional, data, instruções gerais, lista de medicamentos (nome, concentração, posologia, duração, via de administração) e status.
  - `PaymentRecord` / `FinancialTransaction`: Agendamento/consulta relacionada, valor previsto, valor pago, data de pagamento, forma de pagamento (Pix, Dinheiro, Cartão Débito, Cartão Crédito, Transferência), status (Pendente, Pago, Cancelado, Gratuito/Retorno), operador que registrou.
  - `AuditLog`: TenantId, UserId, Ação (Leitura/Criação/Modificação), EntidadeAfetada, RegistroId, Timestamp, Endereço IP.
- [x] **3.3. Multi-Tenancy e DbContext:**
  - Ajustar o Global Query Filter em `ApplicationDbContext` para utilizar expressão segura que avalie dinamicamente o tenant por requisição.
  - Preencher automaticamente `TenantId`, `CreatedAt` e `UpdatedAt` no `SaveChangesAsync`.
- [x] **3.4. Criação e Aplicação de Migrations Versionadas:**
  - Migration `AddCompleteSaasClinicalSchema` gerada e inicialização relacional usa `MigrateAsync()`.
  - Migrations `20260423164609_InitialCreate`, `20260504172915_AddProfessionalIdToAppointment`, `20261009010306_AddCompleteSaasClinicalSchema` e `20261009022731_AddPatientPortalOwnershipAndAppointmentBoundRecords` aplicadas e confirmadas em `__EFMigrationsHistory` no SQL Server do Docker.
  - Constraint `CK_MedicalRecords_AppointmentOrLegacy` confirmada no banco; prontuários legados sem vínculo foram preservados explicitamente.
- [x] **3.5. Commit e Push da Etapa 3:**
  - Testes: `dotnet test ElloSaudeAPI\ElloSaude.sln` (17 unitários e 2 de integração aprovados).
  - Commit `e8334a5` (`feat(backend): concluir modulos clinicos, agenda e financeiro`) publicado em `origin/main`.
  - As alterações posteriores de propriedade do portal e vínculo de prontuário estão cobertas pelas migrations incrementais acima.

---

### ETAPA 4 — IMPLEMENTAÇÃO DOS MÓDULOS DE NEGÓCIO NO BACKEND
**Objetivo:** Desenvolver a lógica dos casos de uso, regras de conflito de agenda, emissão de PDF e financeiro real.

- [x] **4.1. Módulo de Agenda & Disponibilidade:**
  - Casos de uso para configurar disponibilidade semanal, consultar horários livres e criar/cancelar bloqueios de agenda.
  - Validação estrita em `CreateAppointmentCommand`:
    - Impedir agendamento fora do horário de atendimento configurado.
    - Impedir conflito/sobreposição de horário do mesmo médico.
    - Impedir agendamento duplicado para o mesmo paciente no mesmo horário.
- [x] **4.2. Módulo de Consultas e Prontuário Clínico Imutável:**
  - [x] Novos registros exigem agendamento realizado e profissional responsável; registros históricos sem vínculo são identificados como legados e preservados.
  - [x] Acesso ao prontuário restrito a profissionais; secretária e administrador da clínica não recebem acesso na API.
  - [x] Removida a rota de deleção física; registros são assinados ao criar e evoluídos por adendos.
  - [x] Auditoria de criação, leitura e adendos via `AuditLog`.
- [ ] **4.3. Módulo de Receituário Eletrônico & Emissão de PDF:**
  - [x] Comando cria e finaliza receitas vinculadas a consultas realizadas.
  - [x] Geração real de PDF via QuestPDF, compatível com Linux.
  - [x] Download restrito ao profissional emissor; receitas liberadas ao paciente são consultadas por vínculo autenticado `Patient`/`User`.
  - [ ] Validar elegibilidade/licença comercial do QuestPDF antes de produção e implementar assinatura digital válida; o hash atual não equivale a certificado digital.
- [ ] **4.4. Módulo Financeiro Real:**
  - [x] Registrar recebimento e marcar retorno gratuito (`RegisterPaymentCommand`, `MarkAsFreeReturnCommand`).
  - [x] Lançamentos reais com forma de pagamento, pagamentos parciais na mesma forma e relatório por período/profissional/método.
  - [ ] Estorno de valor já recebido ainda não implementado; cancelamento é permitido apenas quando não houve recebimento.
  - [x] Queries para total recebido, saldo pendente, faturamento por profissional, forma de pagamento e contagem de retornos.
- [x] **4.5. Módulo do Portal do Paciente (API):**
  - [x] Vínculo explícito e único entre `Patient.UserId` e `User`; provisionamento somente pela equipe, senha temporária de uso único e troca obrigatória no primeiro login.
  - Endpoints dedicados para o paciente autenticado:
    - [x] `GET /api/patient-portal/my-appointments`: Próximos compromissos e histórico.
    - [x] `POST /api/patient-portal/book`: Agendamento seguro com validação de horários livres.
    - [x] `POST /api/patient-portal/cancel/{id}`: Cancelamento pelo paciente com antecedência mínima de 24 horas.
    - [x] `GET /api/patient-portal/my-prescriptions`: Listagem e download de receitas liberadas.
- [x] **4.6. Commit e Push da Etapa 4:**
  - Testes: `dotnet test ElloSaudeAPI\ElloSaude.sln` (17 unitários e 2 de integração aprovados); `npm --prefix ElloSaudeWeb run build` aprovado.
  - Commit `e8334a5` publicado em `origin/main`; ele consolida as entregas implementadas das etapas 3 e 4.
  - Os itens ainda marcados como pendentes nas etapas 3 e 4 não foram considerados concluídos por esse commit.

---

### ETAPA 5 — FRONTEND PROFISSIONAL (VUE 3 + PRIMEVUE)
**Objetivo:** Atualizar telas existentes, criar os módulos ausentes e proporcionar uma interface SaaS moderna, ágil e responsiva.

- [x] **5.1. Design System & Navegação:**
  - Sidebar dinâmica que se adapta à role do usuário (Secretária, Profissional, Admin, Paciente).
  - Interceptor do `axios.js` direciona corretamente respostas 401 para `/auth/login`.
- [x] **5.2. Aprimoramento da Agenda:**
  - Seleção explícita do médico responsável no modal de agendamento.
  - Filtro da agenda por profissional de saúde.
  - Bloqueio visual e indicação de horários disponíveis.
- [ ] **5.3. Módulo de Prescrições e Receitas:**
  - [x] Formulário dinâmico para adicionar medicamentos, concentrações e posologias, vinculado a consulta realizada e restrito ao profissional responsável.
  - [x] Emissão pela API existente e download do PDF gerado, respeitando a autorização do emissor.
- [x] **5.4. Módulo Financeiro Completo:**
  - Interface com filtros por período e profissional.
  - Tabela detalhada de transações financeiras com status (Pago, Pendente, Retorno Gratuito).
  - Modal para registro de pagamento informando método (Pix, Dinheiro, Cartão).
- [x] **5.5. Portal do Paciente:**
  - Layout limpo e responsivo para o paciente.
  - Visualização de próximos agendamentos e botão de agendar com seleção de especialidade, médico e horário vago.
  - Aba de receitas liberadas com botão de download do PDF.
- [ ] **5.6. Gestão de Equipe e Configurações:**
  - Tela de gestão de profissionais (cadastro de CRM, horários de atendimento).
  - Tela de configurações da clínica e usuários.
- [ ] **5.7. Commit e Push da Etapa 5:**
  - Build: executar `npm run build` em `ElloSaudeWeb`.
  - `git add .`
  - `git commit -m "feat(frontend): novo portal do paciente, emissao de receitas, financeiro profissional e agenda dinamica"`
  - `git push origin main`

---

### ETAPA 6 — SUÍTE DE TESTES AUTOMATIZADOS OBRIGATÓRIOS
**Objetivo:** Comprovar a segurança, isolamento multi-tenant e robustez das regras de negócio.

- [ ] **6.1. Testes Unitários de Negócio:**
  - Criação de agendamentos e bloqueio por sobreposição de horários.
  - Validação de retorno gratuito (sem geração de valor a receber).
  - Cálculos financeiros e somatórios por forma de pagamento.
  - Imutabilidade do prontuário médico.
- [ ] **6.2. Testes de Integração & Isolamento Multi-Tenant:**
  - Teste automatizado comprovando que um usuário da Clínica A **não consegue** visualizar ou alterar pacientes, agendamentos, prontuários ou financeiro da Clínica B, mesmo alterando IDs na requisição.
  - [x] Teste comprovando que usuário com perfil Secretária recebe HTTP 403 Forbidden ao tentar acessar `/api/medicalrecords`.
  - [x] Teste comprovando que um paciente não acessa agendamento alheio e que senha temporária exige troca antes do acesso ao portal.
  - Falta ampliar a cobertura de isolamento para múltiplos tenants e receitas de outros pacientes.
- [ ] **6.3. Commit e Push da Etapa 6:**
  - Execução total da suíte: `dotnet test ElloSaude.sln --logger "console;verbosity=detailed"`.
  - `git add .`
  - `git commit -m "test(coverage): suite completa de testes de isolamento multi-tenant, regras de conflito de agenda e financeiro"`
  - `git push origin main`

---

### ETAPA 7 — AMBIENTE DOCKER DESKTOP E PRODUÇÃO
**Objetivo:** Permitir a subida imediata do ecossistema com um único comando no Docker Desktop.

- [x] **7.1. Dockerfile do Frontend:**
  - Build multi-stage com Node 20 para compilação e Nginx Alpine para servir o SPA com roteamento correto.
- [x] **7.2. Docker Compose Unificado:**
  - Serviços orquestrados: `sqlserver`, `rabbitmq`, `api`, `frontend`.
  - Volume persistente nomeado para dados do banco (`sqlserver_data:/var/opt/mssql/data`).
  - Healthcheck SQL Server via `sqlcmd` validado; API e frontend aguardam dependências saudáveis.
  - Arquivos `.dockerignore` e `.env.example`.
  - Stack iniciada no Docker Desktop; quatro containers saudáveis, endpoint `/health/ready` e frontend validados. Migration e fluxo de conta do portal testados no SQL Server relacional.
- [ ] **7.3. Commit e Push da Etapa 7:**
  - Teste: subir containers com `docker compose up --build -d` e validar conectividade.
  - `git add .`
  - `git commit -m "chore(docker): docker compose unificado com persistencia de dados, healthchecks e frontend nginx"`
  - `git push origin main`

---

### ETAPA 8 — DOCUMENTAÇÃO TÉCNICA EXAUSTIVA E PREPARAÇÃO PARA PRODUÇÃO
**Objetivo:** Garantir transferência de conhecimento completa e auditabilidade técnica.

- [ ] **8.1. Documentação Obrigatória em `docs/`:**
  - `README.md` — Visão geral, início rápido e comandos exatos. O guia de execução da API está atualizado em `ElloSaudeAPI/README.md`.
  - [x] `docs/DOCKER.md` — Guia operacional verificado com os serviços, portas, segredos locais, migrations, healthchecks e preservação do volume.
  - `docs/ARQUITETURA.md` — Camadas, dependências e padrões adotados.
  - `docs/REQUISITOS_FUNCIONAIS.md` — Matriz completa de requisitos e critérios de aceite.
  - `docs/MODELO_DE_DADOS.md` — Diagramas ER, entidades, enums e relacionamentos.
  - `docs/SEGURANCA_E_LGPD.md` — Controles implementados e conformidade técnica.
  - `docs/PERMISSOES_E_ACESSOS.md` — Matriz de autorização por perfil (RBAC).
  - `docs/API_ENDPOINTS.md` — Inventário e contratos de todos os endpoints REST.
  - `docs/DESENVOLVIMENTO.md` — Guia de onboarding para novos desenvolvedores.
  - `docs/TESTES.md` — Relatório de cobertura e instruções de execução.
  - `docs/DOCKER.md` — Instruções operacionais para Docker Desktop.
  - `docs/DEPLOY_PRODUCAO.md` — Checklists de implantação, backups e variáveis de ambiente.
  - `docs/BANCO_DE_DADOS.md` — Estratégia de migrations, backups e recuperação de desastres.
  - `docs/PACOTES_E_DEPENDENCIAS.md` — Justificativa e licenciamento de cada biblioteca.
  - `docs/OPERACAO_E_MONITORAMENTO.md` — Logs, diagnósticos e health checks.
  - `docs/CHANGELOG.md` — Registro histórico de todas as alterações realizadas.
- [ ] **8.2. Commit e Push Final da Etapa 8:**
  - `git add .`
  - `git commit -m "docs(release): documentacao tecnica completa, guias de deploy, operacao e preparacao para producao"`
  - `git push origin main`
