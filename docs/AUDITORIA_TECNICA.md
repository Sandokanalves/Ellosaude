# 📋 RELATÓRIO DE AUDITORIA TÉCNICA E SEGURANÇA — SAAS ELLO SAÚDE

**Data da Auditoria:** 08/10/2026 (revisão incremental)
**Auditor Responsável:** Equipe de Engenharia e Arquitetura de Software / Especialista em Segurança e SaaS Saúde  
**Versão do Projeto Auditado:** MVP em implementação; alterações backend locais ainda não commitadas
**Escopo:** Backend (.NET 9 Web API), Frontend (Vue 3 / Vite), Infraestrutura Docker, Segurança & LGPD, Banco de Dados, Cobertura de Testes.

---

## 1. RESUMO EXECUTIVO

O projeto **ElloSaúde** mantém uma fundação orientada a **Clean Architecture** com .NET 9 no backend e **Vue 3 (Composition API) + PrimeVue** no frontend. Nesta revisão, passaram 17 testes unitários, 2 de integração e o build do frontend.

Ainda não está apto para operação comercial. Há funcionalidades backend novas em andamento, mas o SQL Server não estava acessível para aplicar/verificar a migration; portal do paciente, estorno de valores recebidos, frontend profissional, Docker completo e testes abrangentes continuam pendentes.

### Principais Achados e Pendências:
1. **Segurança & Isolamento:** JWT exige chave com tamanho mínimo; em produção exige `Jwt__Key` do ambiente, e CORS exige origens configuradas. O filtro de tenant é dinâmico por contexto e os testes agora cobrem leitura isolada entre dois tenants. Ainda faltam autorização granular e revisão de BOLA nos endpoints de agenda/paciente.
2. **Financeiro:** Lançamentos reais, formas de pagamento, retorno gratuito e relatório por profissional/método foram implementados. Estorno de pagamentos recebidos e histórico de transações múltiplas ainda faltam.
3. **Módulos:** Agenda/disponibilidade e receituário com PDF foram implementados parcialmente; portal do paciente está bloqueado pela ausência de vínculo explícito `Patient`–`User`. Gestão completa de usuários e configurações segue ausente.
4. **Inicialização:** A API aplica migrations em provedores relacionais e limita o seed a desenvolvimento. A migration nova não pôde ser aplicada/verificada porque o SQL Server estava indisponível.
5. **Prontuário:** Exclusão física e edição direta foram removidas da API; registros são assinados ao criar, evoluções são adendos e os acessos são auditados. A associação obrigatória do atendimento a um agendamento e a cobertura de autorização ainda precisam ser concluídas.
6. **Infraestrutura/Testes:** Docker ainda não inclui frontend/persistência/healthchecks completos. Nesta revisão passaram 17 testes unitários, 2 de integração e o build Vue; cobertura de negócio e isolamento ainda é insuficiente para produção.

> As análises detalhadas abaixo descrevem o baseline da auditoria inicial; a matriz da seção 5 e as pendências acima refletem o estado revisado em 08/10/2026.

---

## 2. AUDITORIA DETALHADA DO BACKEND (.NET 9)

### 2.1. Identificação Técnica
- **Versão do Target Framework:** .NET 9.0 (`net9.0`) em todos os projetos C#.
- **SDKs Detectados no Host:** .NET SDK 10.0.203, 9.0.313, 8.0.408. Compilação bem-sucedida.
- **Estrutura da Solution (`ElloSaude.sln`):**
  - `ElloSaude.Domain`: Entidades e Enums.
  - `ElloSaude.Application`: Casos de uso MediatR, DTOs, Mappings, Interfaces.
  - `ElloSaude.Infrastructure`: EF Core DbContext, Repositórios, JWT, BCrypt, RabbitMQ publisher.
  - `ElloSaude.Api`: Controllers REST, Middlewares, Program.cs.
  - `ElloSaude.UnitTests`: 4 testes unitários.
  - `ElloSaude.IntegrationTests`: 1 teste de integração.

### 2.2. Avaliação por Camada e Componentes

#### A. Camada de Domínio (`ElloSaude.Domain`)
- **`BaseEntity.cs`:**
  - Possui `Id` (Guid) e `TenantId` (string).
  - *Problema:* Ausência de timestamps de auditoria (`CreatedAt`, `UpdatedAt`), controle de concorrência (`RowVersion`/`byte[]`) e suporte a Soft Delete (`IsDeleted`, `DeletedAt`).
- **`User.cs`:**
  - Contém `Email`, `PasswordHash`, `Role`, `TenantId`.
  - *Problema:* Não implementa ASP.NET Core Identity. Não suporta múltiplos papéis, claims granulares, status (Ativo/Inativo), tentativas falhas de login (lockout) ou vínculo explícito com dados profissionais (CRM, especialidades).
- **`Patient.cs`:**
  - Contém `Name`, `Email`, `Cpf`, `BirthDate`, `TenantId`, `Records`.
  - *Problema:* Faltam dados essenciais: telefone de contato, endereço, responsável/contato de emergência, sexo biológico e status de cadastro.
- **`Appointment.cs`:**
  - Contém `StartTime`, `EndTime`, `PatientId`, `ProfessionalId`, `Status`, `Type`.
  - *Problema:* Não possui campos para valor previsto, valor cobrado, observações de agendamento, modalidade (presencial/telemedicina), histórico de alterações de status e cancelamento justificado.
- **`MedicalRecord.cs`:**
  - Contém `PatientId`, `DoctorId`, `Description`, `Diagnosis`, `CreatedAt`.
  - *Problema:* Método `Update(description, diagnosis)` sobrescreve silenciosamente o registro original. Falta número de versão, controle de assinatura eletrônica e estrutura para adendos clínicos imutáveis.
- **Entidades Faltantes:**
  - `Professional` / `Doctor` (vínculo clínico, CRM, especialidade, disponibilidade).
  - `ScheduleBlock` / `Availability` (grade de atendimento e bloqueios/férias).
  - `Prescription` & `PrescriptionItem` (receituário, medicamentos, posologia, instruções, hash/PDF).
  - `FinancialTransaction` / `PaymentRecord` (registro financeiro real, método de pagamento Pix/dinheiro/cartão, status de quitação, indicador de retorno gratuito).
  - `AuditLog` (registro de acessos e modificações de prontuário para LGPD).

#### B. Camada de Aplicação (`ElloSaude.Application`)
- **MediatR & CQRS:**
  - Handlers implementados para Patients, Appointments, MedicalRecords e FinancialSummary.
  - *Falha Crítica no FluentValidation:* Há validadores criados em `Commands` e `Validators`, porém **não há registro de `IPipelineBehavior<,>` para validação automática**. Se uma requisição com dados inválidos for enviada, os validadores não são disparados antes do handler.
- **Regras de Negócio na Agenda:**
  - `CreateAppointmentHandler`: Insere o agendamento diretamente no banco sem checar se o profissional já possui outro agendamento no mesmo horário (conflito/sobreposição). Não valida se o agendamento cai dentro do horário de atendimento configurado.
  - Não valida se o paciente já possui outro agendamento no mesmo horário.
- **Módulo Financeiro:**
  - `GetFinancialSummaryHandler`: Implementação falsa baseada em contagem de agendamentos multiplicada por R$ 150,00 (`consultationPrice = 150.00m`). Não reflete a realidade financeira da clínica.
- **Isolamento de Tenants:**
  - A maioria dos comandos não valida se as chaves estrangeiras passadas (`PatientId`, `ProfessionalId`) de fato pertencem ao tenant do usuário autenticado antes de salvar.

#### C. Camada de Infraestrutura (`ElloSaude.Infrastructure`)
- **`ApplicationDbContext.cs`:**
  - Aplica `HasQueryFilter` dinâmico em entidades que herdam de `BaseEntity`.
  - *Risco no EF Core:* Usa `Expression.Constant(_tenantService)`. O modelo construído pelo EF Core captura a instância de `_tenantService` gerada no primeiro build do modelo.
  - `SaveChanges`/`SaveChangesAsync`: Preenche o `TenantId` em entidades adicionadas se estiver vazio.
- **`DbInitializer.cs`:**
  - Utiliza `context.Database.EnsureCreatedAsync()`, o que ignora e desativa o fluxo seguro de migrations versionadas do EF Core em bases com histórico de migração.
  - Cria usuários e clínicas padrão com dados hardcoded.
- **Mensageria RabbitMQ:**
  - `RabbitMqService.cs` implementa `IMessageBusService` e publica mensagens com `BasicPublish`. Porém, não existem consumidores (consumers/background workers) implementados no sistema.

#### D. Camada de Apresentação e API (`ElloSaude.Api`)
- **`Program.cs`:**
  - Chave JWT de fallback insegura embutida no código.
  - Não valida `Issuer` nem `Audience` (`ValidateIssuer = false`, `ValidateAudience = false`).
  - CORS aberto sem restrição (`AllowAnyOrigin`, `AllowAnyMethod`, `AllowAnyHeader`).
  - Ausência de Health Checks (`AddHealthChecks()`).
  - Ausência de Rate Limiting.
- **Tratamento Global de Exceções (`ExceptionMiddleware.cs`):**
  - Captura qualquer exceção e retorna HTTP 500 com `{ error = "Erro Interno", message = ex.Message }`.
  - Não implementa RFC 7807 (Problem Details).
  - Expõe detalhes internos de exceção para o cliente. Não diferencia erros de validação (400), não encontrados (404) e acessos negados (403).
- **Controle de Acesso nos Controllers:**
  - `AppointmentsController`: Apenas `[Authorize]`. Paciente e Secretária podem chamar endpoints destrutivos (`DELETE /api/appointments/{id}`).
  - `MedicalRecordsController`: Permite `Roles = "Profissional, Admin"`. O perfil `Admin` não deveria ter acesso irrestrito ao conteúdo clínico de prontuários.
  - Endpoints de download seguro de documentos médicos e receitas não existem.

---

## 3. AUDITORIA DETALHADA DO FRONTEND (VUE 3)

### 3.1. Identificação Técnica
- **Stack:** Vue.js 3.4.21, Vite 5.2.8, PrimeVue 3.52.0, PrimeIcons 7.0.0, PrimeFlex 3.3.1, FullCalendar 6.1.18, Pinia 2.1.7, Axios 1.7.2, VeeValidate 4.13.1 + Zod 3.23.8.
- **Linguagem:** JavaScript (ES Modules). Não está em TypeScript.
- **Estado dos Builds:** `npm run build` executa sem erros de sintaxe (bundle gerado na pasta `dist`).

### 3.2. Análise de Páginas e Telas
1. **Login (`LoginPage.vue`):**
   - Formulário com e-mail e senha.
   - Integração real com `/api/auth/login`. Salva token e objeto de usuário no `localStorage`.
   - *Bug identificado no interceptor `axios.js`:* Em caso de erro 401, redireciona para `window.location.href = '/login'`, enquanto a rota real do Vue Router é `/auth/login`.
2. **Agenda (`AgendaPage.vue`):**
   - Utiliza FullCalendar com visualização semanal e mensal.
   - Carrega eventos de `/api/appointments`.
   - *Problema Funcional:* No formulário de novo agendamento, atribui o agendamento fixamente ao `authStore.user?.id` ou `'00000000-0000-0000-0000-000000000000'`. Não permite que a secretária selecione o médico/profissional desejado.
   - Não exibe nem filtra disponibilidade, pausas ou horários de atendimento dos médicos.
3. **Gestão de Pacientes (`PatientsListPage.vue`):**
   - Tabela PrimeVue com busca, paginação e modal de cadastro/edição.
   - Integrada com `/api/patients`.
   - *Problema:* Paginação é puramente client-side; busca todos os pacientes do banco de uma só vez, o que não escala para bases com milhares de pacientes.
4. **Prontuário & Ficha do Paciente (`PatientDetailPage.vue`):**
   - Exibe timeline de prontuário com diagnósticos e condutas.
   - Bloqueia visualização se `!authStore.isDoctor`.
   - *Gaps:* Texto fixo `"Médico Responsável: Dr. Profissional"`. Não exibe histórico de agendamentos do paciente (RF08). Não permite emitir receitas nem anexar documentos.
5. **Painel Financeiro (`FinancialPage.vue`):**
   - Exibe 3 cards (Total Recebido, Total Pendente, Faturamento Total) baseados no cálculo estático de R$ 150 por consulta.
   - Ticket médio estático chumbado no template: `"R$ 150,00"`.
   - Não permite registrar pagamentos, emitir recibos, visualizar transações por forma de pagamento ou lançar consultas de retorno sem cobrança.
6. **Configurações (`SettingsPage.vue`):**
   - Tela vazia com apenas um placeholder: `"Opções de configuração da conta e do sistema serão implementadas aqui."`
7. **Telas Totalmente Ausentes no Frontend:**
   - Tela de Emissão e Visualização de Receitas Médicas.
   - Portal do Paciente (Home do paciente, meus agendamentos, agendar consulta, minhas receitas).
   - Tela de Gestão de Profissionais e Especialidades (horários de atendimento, bloqueios).
   - Tela de Gestão de Usuários da Clínica e Permissões.
   - Tela de Relatórios Operacionais e Financeiros Detalhados.

---

## 4. AUDITORIA DA INFRAESTRUTURA E DOCKER

### 4.1. Configuração Atual
- Arquivo `ElloSaudeAPI/docker-compose.yml`:
  - Serviço `sqlserver`: Imagem `mcr.microsoft.com/mssql/server:2022-latest`.
  - Serviço `rabbitmq`: Imagem `rabbitmq:3-management`.
  - Serviço `api`: Build de `ElloSaude.Api/Dockerfile`.

### 4.2. Vulnerabilidades e Falhas de Execução
1. **Falta do Frontend no Docker Compose:** Não existe serviço para o `ElloSaudeWeb` no compose, obrigando execução manual com Node.js na máquina host.
2. **Perda de Dados no Banco:** O serviço `sqlserver` não declara volumes mapeados para `/var/opt/mssql/data`. Se o container for recriado, todos os dados do banco e cadastros das clínicas são perdidos.
3. **Condição de Corrida na Inicialização:** O serviço `api` possui apenas `depends_on: - sqlserver`, sem teste de integridade/healthcheck (`condition: service_healthy`). Como o SQL Server leva até 20 segundos para aceitar conexões TCP após subir o container, a API falha ao inicializar o banco de dados.
4. **Segredos Hardcoded:** Senhas de banco e chave JWT estão expostas no `docker-compose.yml`. Não existe arquivo `.env` nem `.env.example`.
5. **Arquivos `.dockerignore`:** Inexistentes tanto no backend quanto no frontend, enviando `bin`, `obj`, `node_modules` e pastas locais para o contexto de build do Docker.

---

## 5. MATRIZ DE REQUISITOS E CLASSIFICAÇÃO TÉCNICA

Abaixo está o inventário de cada módulo do sistema com sua respectiva classificação conforme solicitado no Prompt Mestre:

| Módulo / Requisito | Status Atual | Evidências no Código | Ações Necessárias |
| :--- | :--- | :--- | :--- |
| **Arquitetura Base .NET 9** | IMPLEMENTADO E TESTADO | Solution compila, Clean Architecture dividida em Domain, App, Infra, Api. | Manter estrutura e padronizar injeção de dependências. |
| **Isolamento Multi-Tenant (SaaS)** | IMPLEMENTADO PARCIALMENTE | Filtro global dinâmico por contexto, `TenantId` preenchido no save e consultas por chave via LINQ; teste cobre dois tenants. | Ampliar testes para todos os recursos, corrigir BOLA em agenda/pacientes e verificar migrations com SQL Server. |
| **Autenticação de Usuários** | IMPLEMENTADO PARCIALMENTE | JWT valida emissor, público e expiração; produção exige chave de ambiente com tamanho mínimo. Senhas usam BCrypt. | Implementar recuperação de acesso, rotação/refresh, rate limit e revisar credenciais legadas já presentes na configuração versionada. |
| **Autorização & Matriz de Permissões** | IMPLEMENTADO PARCIALMENTE | Roles básicas e verificação de proprietário para prontuário/receita; Admin e Secretaria bloqueados no prontuário clínico. | Criar políticas granulares e auditar todos os endpoints, especialmente mutações de pacientes/agendamentos. |
| **Cadastro de Pacientes** | IMPLEMENTADO, MAS SEM TESTES SUFICIENTES | Entidade ampliada com telefone/endereço/contato de emergência/status; endpoints atuais continuam sem paginação backend. | Implementar paginação e ampliar testes de autorização e validação. |
| **Agenda e Horários** | IMPLEMENTADO PARCIALMENTE | Bloqueia ausência de disponibilidade, intervalos fora da grade, pausas, bloqueios e sobreposição; testes cobrem conflitos do profissional. | Validar concorrência simultânea, conflito do paciente, timezone e histórico de status. |
| **Disponibilidade dos Médicos** | IMPLEMENTADO PARCIALMENTE | `DoctorAvailability` e `ScheduleBlock`, endpoints de configuração/slots e criação/cancelamento de bloqueios. | Ampliar testes de autorização e suportar múltiplas janelas por dia, se necessário ao negócio. |
| **Prontuário Eletrônico** | IMPLEMENTADO PARCIALMENTE | Sem DELETE/PUT na API; criação assina o registro, alterações posteriores são adendos; leituras/criações são auditadas e acesso restrito ao profissional autor. | Vincular cada atendimento obrigatoriamente a agendamento e testar acesso entre profissionais da mesma clínica. |
| **Receituário Eletrônico & PDF** | IMPLEMENTADO PARCIALMENTE | Entidades, comando associado a consulta realizada, QuestPDF, PDF real e download restrito ao profissional emissor; geração testada. | Vínculo com paciente autenticado, validar licença comercial e implementar assinatura digital certificada. |
| **Controle Financeiro de Consultas** | IMPLEMENTADO PARCIALMENTE | Lançamentos reais, recebimentos, métodos, parcelas na mesma forma, saldo pendente e relatório por profissional/método. | Implementar estorno de valor recebido, histórico de transações e testes de relatório/período. |
| **Consulta de Retorno Gratuito** | IMPLEMENTADO PARCIALMENTE | `IsFreeReturn` zera a cobrança no agendamento e `PaymentRecord` protege lançamentos já pagos. | Testar o fluxo ponta a ponta e preservar valor original para relatórios de retorno sem cobrança. |
| **Portal do Paciente** | NÃO IMPLEMENTADO | Não há relação explícita entre `User` autenticado e `Patient`; os endpoints não podem inferir titularidade com segurança. | Modelar associação explícita e fluxo de ativação antes de criar endpoints/telas do portal; nunca associar por e-mail. |
| **Gestão de Usuários da Clínica** | NÃO IMPLEMENTADO | Apenas seed cria usuários. Não há endpoints para listar, convidar ou desativar colaboradores da clínica. | Criar endpoints e telas de gestão de equipe clínica. |
| **Validação de Entrada (FluentValidation)** | IMPLEMENTADO E TESTADO | Pipeline MediatR registra validadores; suíte testa rejeição de requisições inválidas. | Cobrir todos os comandos e contratos HTTP. |
| **Tratamento Global de Erros** | IMPLEMENTADO E TESTADO | Middleware responde `ProblemDetails`/`ValidationProblemDetails` com 400/403/404/409/500 e não expõe detalhes internos em produção. | Adicionar testes de integração específicos de cada status. |
| **Docker Completo (Dev/Prod)** | IMPLEMENTADO PARCIALMENTE | Dockerfile da API e docker-compose com SQL Server/RabbitMQ existem, mas faltam frontend, volumes persistentes e healthchecks. | Reestruturar Docker Compose com persistência, healthcheck e frontend com Nginx. |
| **Conformidade LGPD & Auditoria** | IMPLEMENTADO PARCIALMENTE | `AuditLog` registra criação/leitura de prontuários e emissão/leitura de receitas. | Auditar demais acessos a dados pessoais, definir retenção/consentimento e revisar acesso excepcional. |
| **Testes Automatizados (Cobertura)** | IMPLEMENTADO, MAS SEM TESTES SUFICIENTES | 17 testes unitários e 2 de integração passaram; inclui tenant filter, agenda, PDF, pagamentos e acesso de Secretaria. Frontend sem suíte. | Ampliar integração multi-tenant para todas as entidades, testar financeiro/prontuário e adicionar testes frontend. |

---

## 6. DIAGNÓSTICO DE RISCOS DE SEGURANÇA

1. **Autorização e isolamento de prontuários:**
   - O acesso foi restringido ao profissional autor e as rotas de edição/exclusão física foram removidas. Ainda é necessário ampliar testes de autorização entre profissionais e provar o isolamento por tenant em todos os recursos clínicos.
2. **Risco de IDOR / BOLA (Broken Object Level Authorization):**
   - A autorização granular de agenda, pacientes e financeiro ainda requer revisão e testes por papel/tenant; os testes atuais não cobrem todos os endpoints e operações.
3. **Exposição de Credenciais e Segredos:**
   - Arquivos de configuração versionados ainda contêm valores de credenciais/chave. Não publicar esses valores; externalizá-los, revogar e rotacionar qualquer segredo real antes de publicar o repositório.
4. **Integridade clínica e assinatura:**
   - Edições diretas foram substituídas por adendos auditados, mas a associação do prontuário a um agendamento continua opcional e o hash de receita não equivale a uma assinatura digital certificada.

---

## 7. CONCLUSÃO DA AUDITORIA

O projeto **ElloSaúde** fornece uma base funcional mínima para demonstração de conceito (POC), mas **não está apto para operação comercial nem pronto para produção**.

Para se tornar um produto SaaS comercializável, seguro e robusto, o sistema precisa:
1. Externalizar/revogar segredos versionados e concluir revisão de autorização granular, mantendo JWT, validação e tratamento de erros como fundações implementadas.
2. Concluir o vínculo obrigatório do prontuário com agendamento, assinatura digital adequada e estorno financeiro.
3. Projetar o vínculo explícito `Patient`–`User` antes de implementar o Portal do Paciente; não inferir titularidade por e-mail.
4. Completar o frontend clínico/financeiro e unificar o ambiente Docker com persistência e execução ponta a ponta (Frontend + Backend + DB).
5. Ampliar os testes automatizados de autorização, isolamento multi-tenant, regras de negócio e frontend; aplicar a migration em SQL Server para validar o caminho relacional.
