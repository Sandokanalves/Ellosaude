# 📋 RELATÓRIO DE AUDITORIA TÉCNICA E SEGURANÇA — SAAS ELLO SAÚDE

**Data da Auditoria:** Outubro de 2026  
**Auditor Responsável:** Equipe de Engenharia e Arquitetura de Software / Especialista em Segurança e SaaS Saúde  
**Versão do Projeto Auditado:** 0.0.1 (MVP Inicial)  
**Escopo:** Backend (.NET 9 Web API), Frontend (Vue 3 / Vite), Infraestrutura Docker, Segurança & LGPD, Banco de Dados, Cobertura de Testes.

---

## 1. RESUMO EXECUTIVO

O projeto **ElloSaúde** possui uma fundação arquitetural preliminar orientada a **Clean Architecture** com .NET 9 no backend e **Vue 3 (Composition API) + PrimeVue** no frontend. 

No entanto, a auditoria revelou que o projeto encontra-se em estágio embrionário (protótipo/MVP básico) com **severos gaps de completude funcional, ausência de módulos críticos exigidos para comercialização, falhas graves de segurança/isolamento de dados, mecanismos fictícios/estáticos no controle financeiro e carência crítica de testes automatizados**.

### Principais Achados Críticos:
1. **Segurança & Falhas de Isolamento:** Chaves criptográficas JWT e senhas de banco de dados (`SuaSenhaForte123!`) estão expostas em texto claro nos arquivos de configuração e repositório. O isolamento multi-tenant depende exclusivamente de um Global Query Filter do EF Core sem validação explícita de autorização por políticas nos Controllers ou nos handlers de comandos.
2. **Módulo Financeiro Simulado:** O cálculo financeiro não possui tabelas, lançamentos, recibos ou baixas. O sistema simplesmente conta o número de agendamentos com status "Realizado" e multiplica por R$ 150,00 fixos hardcoded no código (`consultationPrice = 150.00m`). Não há suporte a consultas de retorno gratuitas, formas de pagamento ou relatórios por profissional.
3. **Módulos Ausentes:** Não existem implementações de **Receituário Eletrônico com Geração de PDF**, **Portal do Paciente**, **Disponibilidade/Bloqueios de Agenda do Médico**, **Gestão de Usuários e Permissões** e **Configurações da Clínica**.
4. **Validações Inoperantes:** As classes do FluentValidation (`CreateAppointmentValidator`, `CreatePatientValidator`, etc.) foram criadas no projeto `ElloSaude.Application`, mas **não estão registradas** no pipeline do ASP.NET Core nem como MediatR Pipeline Behavior. Nenhuma validação é executada automaticamente nas requisições da API.
5. **Prontuário Médico sem Integridade e com Risco Regulatório:** Registros de prontuário podem ser editados e sobrescritos diretamente por qualquer médico ou admin (`UpdateMedicalRecordCommand`), sem versionamento, sem histórico de adendos e com endpoint de deleção física permanente (`DELETE /api/medicalrecords/{id}`), violando normas do CFM e LGPD.
6. **Infraestrutura Docker Incompleta:** O `docker-compose.yml` não inclui o container do frontend, não possui volumes persistentes para o SQL Server (risco de perda total de dados) e não possui healthchecks para garantir que a API aguarde o banco de dados inicializar.

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
| **Isolamento Multi-Tenant (SaaS)** | IMPLEMENTADO COM FALHAS DE SEGURANÇA | `ApplicationDbContext.cs` possui Query Filter, mas controllers não validam tenant em parâmetros/chaves estrangeiras; `TenantId` em claims JWT sem validação de políticas. | Implementar validação de tenant rigorosa em commands/queries e nos middlewares. |
| **Autenticação de Usuários** | IMPLEMENTADO PARCIALMENTE | `AuthController.cs`, `UserService.cs` geram JWT com BCrypt. | Adicionar expiração segura, refresh tokens, validação de Issuer/Audience e proteção contra força bruta. |
| **Autorização & Matriz de Permissões** | NÃO IMPLEMENTADO | Controllers usam apenas `[Authorize]` ou roles simples hardcoded (`Profissional`, `Admin`). Não há políticas ou claims granulares. | Criar políticas de autorização baseadas no princípio do menor privilégio (`patients.read`, `medical_records.read`, etc.). |
| **Cadastro de Pacientes** | IMPLEMENTADO, MAS SEM TESTES SUFICIENTES | `PatientsController.cs`, `Patient.cs` existem, mas faltam campos (telefone, endereço, contato emergência) e paginação no backend. Apenas 1 teste de integração básico. | Completar campos da entidade, implementar paginação com DTO no backend e testes de validação. |
| **Agenda e Horários** | IMPLEMENTADO PARCIALMENTE | `AppointmentsController.cs` e `AgendaPage.vue` funcionam com FullCalendar, mas não checam sobreposição de horários nem disponibilidade médica. | Implementar algoritmo de detecção de conflitos de agenda e tabela de disponibilidade dos médicos. |
| **Disponibilidade dos Médicos** | NÃO IMPLEMENTADO | Não existem entidades de horário de atendimento, pausas, folgas ou bloqueios de agenda. | Modelar e implementar `DoctorAvailability` e regras de validação no agendamento. |
| **Prontuário Eletrônico** | IMPLEMENTADO COM FALHAS DE SEGURANÇA | `MedicalRecordsController.cs` permite deleção permanente de prontuários (`DELETE`) e alteração direta (`PUT`) sem trilha de adendos; Admin tem acesso ao prontuário. | Bloquear exclusão de prontuários, implementar modelo imutável com adendos e segregar permissão médica de administrativa. |
| **Receituário Eletrônico & PDF** | NÃO IMPLEMENTADO | Inexistente no backend e no frontend. Nenhuma biblioteca de PDF configurada. | Modelar entidade `Prescription`, criar gerador de PDF compatível com Linux/Docker (QuestPDF) e endpoints protegidos. |
| **Controle Financeiro de Consultas** | IMPLEMENTADO COM FALHAS DE SEGURANÇA | `FinancialController.cs` calcula valores fictícios baseados em R$ 150 fixos multiplicados por consultas realizadas. Não registra pagamentos nem formas de pagamento. | Criar entidade `PaymentRecord`/`FinancialTransaction`, suporte a Pix/dinheiro/cartão, status de quitação e histórico real. |
| **Consulta de Retorno Gratuito** | NÃO IMPLEMENTADO | Retorno é apenas um enum sem lógica de negócio; agendamento de retorno é cobrado R$ 150 no cálculo financeiro. | Implementar regra explícita de retorno gratuito sem geração de contas a receber. |
| **Portal do Paciente** | NÃO IMPLEMENTADO | Inexistente no backend e no frontend. Pacientes não conseguem visualizar agenda, agendar ou baixar receitas. | Desenvolver layout e endpoints exclusivos do portal do paciente com isolamento rigoroso. |
| **Gestão de Usuários da Clínica** | NÃO IMPLEMENTADO | Apenas seed cria usuários. Não há endpoints para listar, convidar ou desativar colaboradores da clínica. | Criar endpoints e telas de gestão de equipe clínica. |
| **Validação de Entrada (FluentValidation)** | IMPLEMENTADO PARCIALMENTE | Classes validadoras existem, mas não estão plugadas no pipeline de execução do ASP.NET Core/MediatR. | Adicionar `ValidationBehavior` no MediatR para disparar validações antes dos handlers. |
| **Tratamento Global de Erros** | IMPLEMENTADO PARCIALMENTE | `ExceptionMiddleware.cs` captura exceções, mas devolve 500 para tudo e não usa Problem Details (RFC 7807). | Implementar middleware de Problem Details com códigos de erro HTTP semânticos (400, 403, 404, 409, 500). |
| **Docker Completo (Dev/Prod)** | IMPLEMENTADO PARCIALMENTE | Dockerfile da API e docker-compose com SQL Server/RabbitMQ existem, mas faltam frontend, volumes persistentes e healthchecks. | Reestruturar Docker Compose com persistência, healthcheck e frontend com Nginx. |
| **Conformidade LGPD & Auditoria** | NÃO IMPLEMENTADO | Não há tabela de auditoria de acessos a dados sensíveis, consentimentos ou logs estruturados de auditoria. | Criar `AuditLog` para rastrear quem visualizou ou alterou prontuários e dados de pacientes. |
| **Testes Automatizados (Cobertura)** | IMPLEMENTADO, MAS SEM TESTES SUFICIENTES | Apenas 4 testes unitários e 1 de integração no backend. 0 testes no frontend. Sem testes multi-tenant ou de autorização. | Expandir suíte com testes de isolamento multi-tenant, validação de regras de agenda, financeiro e autorização. |

---

## 6. DIAGNÓSTICO DE RISCOS DE SEGURANÇA

1. **Risco de Acesso Indevido a Prontuários (LGPD / CFM):**
   - Usuários com papel `Admin` têm permissão no `MedicalRecordsController` para ler o prontuário de qualquer paciente. A regra de negócio e regulatória exige que secretárias e administradores de clínica não tenham acesso clínico a prontuários e diagnósticos.
2. **Risco de IDOR / BOLA (Broken Object Level Authorization):**
   - No `AppointmentsController`, um usuário autenticado pode passar o ID de qualquer consulta e emitir um `DELETE` ou `PATCH` de status sem validação prévia de vínculo ou permissão do profissional.
3. **Exposição de Credenciais e Segredos:**
   - Senhas de banco de dados e chave JWT estão presentes no repositório (`appsettings.json`, `docker-compose.yml`). É mandatória a externalização via variáveis de ambiente (`.env`).
4. **Violação de Integridade Clínica:**
   - A possibilidade de alterar diretamente o texto de uma evolução clínica existente sem gerar uma nova versão ou registro de adendo representa grave risco de conformidade e legalidade para clínicas que utilizarem o sistema.

---

## 7. CONCLUSÃO DA AUDITORIA

O projeto **ElloSaúde** fornece uma base funcional mínima para demonstração de conceito (POC), mas **não está apto para operação comercial nem pronto para produção**. 

Para se tornar um produto SaaS comercializável, seguro e robusto, o sistema precisa:
1. Corrigir as fundações de segurança (JWT, segregação de segredos, validações automáticas, tratamento de erros RFC 7807).
2. Modelar e persistir os módulos reais de Financeiro, Receituário Eletrônico (com PDF), Disponibilidade de Agenda e Portal do Paciente.
3. Blindar os prontuários eletrônicos com imutabilidade e trilha de auditoria.
4. Unificar o ambiente Docker com persistência e execução ponta a ponta (Frontend + Backend + DB).
5. Implementar uma ampla bateria de testes automatizados comprovando o isolamento entre clínicas.
