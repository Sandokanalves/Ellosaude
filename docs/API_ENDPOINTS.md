# 🔗 INVENTÁRIO DE ENDPOINTS — API ELLO SAÚDE

**Base URL:** `https://localhost:7001` (dev) | `http://api:8080` (Docker)  
**Autenticação:** `Authorization: Bearer <jwt_token>` (exceto `/api/auth/login`)  
**Formato:** JSON (`Content-Type: application/json`)  
**Erros:** RFC 7807 ProblemDetails (`type`, `title`, `status`, `detail`, `errors`)

---

## 🔑 AUTH — Autenticação

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `POST` | `/api/auth/login` | Público | Login com e-mail e senha; retorna `{ token, role, tenantId }` |
| `POST` | `/api/auth/change-password` | Todos | Troca de senha (obrigatória para senhas temporárias) |

### POST /api/auth/login
**Body:**
```json
{ "email": "admin@ellosaude.com", "password": "SuaSenha123!" }
```
**Response 200:**
```json
{ "token": "eyJ...", "role": "Admin", "tenantId": "clinic-abc", "mustChangePassword": false }
```

---

## 👥 PATIENTS — Pacientes

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `GET` | `/api/patients` | Admin, Profissional, Secretaria | Lista pacientes do tenant com filtro por nome/CPF |
| `POST` | `/api/patients` | Admin, Secretaria | Cria novo paciente |
| `GET` | `/api/patients/{id}` | Admin, Profissional, Secretaria | Retorna paciente por ID |
| `PUT` | `/api/patients/{id}` | Admin, Profissional, Secretaria | Atualiza dados cadastrais |
| `DELETE` | `/api/patients/{id}` | Admin | Soft delete do paciente |
| `POST` | `/api/patients/{id}/portal-account` | Admin, Secretaria | Cria conta no portal; retorna senha temporária |
| `PUT` | `/api/patients/{id}/clinical-summary` | Profissional | Atualiza resumo clínico (alergias, medicações, condições) |

---

## 📅 APPOINTMENTS — Agendamentos

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `GET` | `/api/appointments` | Admin, Profissional, Secretaria | Lista agendamentos (filtros: data, profissionalId) |
| `POST` | `/api/appointments` | Admin, Profissional, Secretaria | Cria agendamento |
| `PUT` | `/api/appointments/{id}/status` | Admin, Profissional, Secretaria | Atualiza status do agendamento |
| `DELETE` | `/api/appointments/{id}` | Admin, Secretaria | Cancela agendamento |

---

## 👨‍⚕️ PROFESSIONALS — Profissionais

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `GET` | `/api/professionals` | Admin, Profissional, Secretaria | Lista profissionais ativos |
| `POST` | `/api/professionals` | Admin | Cadastra profissional |
| `PUT` | `/api/professionals/{id}` | Admin | Atualiza profissional |
| `GET` | `/api/professionals/{id}/availability` | Admin, Profissional, Secretaria | Retorna disponibilidade semanal |
| `POST` | `/api/professionals/{id}/availability` | Profissional | Configura disponibilidade semanal |
| `GET` | `/api/professionals/{id}/available-slots` | Admin, Profissional, Secretaria, Paciente | Retorna horários livres para uma data |
| `GET` | `/api/professionals/schedule-blocks` | Profissional | Lista bloqueios de agenda do profissional |
| `POST` | `/api/professionals/schedule-blocks` | Profissional | Cria bloqueio de agenda |
| `DELETE` | `/api/professionals/schedule-blocks/{id}` | Profissional | Remove bloqueio de agenda |

---

## 📋 MEDICAL RECORDS — Prontuários

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `GET` | `/api/medicalrecords/patient/{patientId}` | Profissional | Lista prontuários do paciente |
| `POST` | `/api/medicalrecords` | Profissional | Cria novo prontuário (vinculado a consulta realizada) |
| `POST` | `/api/medicalrecords/{id}/addendum` | Profissional | Adiciona adendo ao prontuário |

> **Secretaria recebe HTTP 403 em qualquer endpoint de prontuário.**

---

## 💊 PRESCRIPTIONS — Receituário

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `GET` | `/api/prescriptions` | Profissional | Lista receitas emitidas pelo profissional autenticado |
| `POST` | `/api/prescriptions` | Profissional | Cria receita com itens de medicamentos |
| `PUT` | `/api/prescriptions/{id}/finalize` | Profissional | Finaliza receita (não pode mais ser editada) |
| `GET` | `/api/prescriptions/{id}/pdf` | Profissional, Paciente (portal) | Download do PDF da receita |

---

## 💰 FINANCIAL — Financeiro

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `GET` | `/api/financial/pending` | Admin, Profissional, Secretaria | Lista cobranças pendentes |
| `GET` | `/api/financial/report` | Admin, Profissional | Relatório detalhado com filtros (período, profissional, método) |
| `POST` | `/api/financial/{id}/payment` | Admin, Secretaria | Registra pagamento |
| `POST` | `/api/financial/{id}/free-return` | Admin, Secretaria | Marca como retorno gratuito |
| `DELETE` | `/api/financial/{id}` | Admin | Cancela cobrança (apenas se sem pagamento) |

---

## 🏥 CLINICS — Configurações da Clínica

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `GET` | `/api/clinics/my` | Admin | Retorna dados da clínica do tenant autenticado |
| `PUT` | `/api/clinics/my` | Admin | Atualiza dados da clínica (logo, nome, políticas) |

---

## 🏠 PATIENT PORTAL — Portal do Paciente

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `GET` | `/api/patient-portal/my-appointments` | Paciente | Próximos e histórico de agendamentos |
| `POST` | `/api/patient-portal/book` | Paciente | Agenda consulta |
| `POST` | `/api/patient-portal/cancel/{id}` | Paciente | Cancela consulta (mín. 24h antecedência) |
| `GET` | `/api/patient-portal/my-prescriptions` | Paciente | Lista receitas liberadas |
| `GET` | `/api/patient-portal/my-appointments/{id}` | Paciente | Detalhe de agendamento (retorna 404 se não for do paciente) |

---

## 🔔 NOTIFICATIONS — Notificações

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `GET` | `/api/notifications` | Profissional | Lista notificações da sala de espera |
| `POST` | `/api/notifications/{id}/read` | Profissional | Marca notificação como lida |

---

## 🩺 MEDICAL DOCUMENTS — Documentos Clínicos

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `POST` | `/api/medicaldocuments` | Profissional | Upload de documento/exame anexo ao paciente |
| `GET` | `/api/medicaldocuments/patient/{patientId}` | Profissional | Lista documentos do paciente |

---

## 🗂️ USERS — Gestão de Usuários

| Método | Rota | Roles | Descrição |
|---|---|---|---|
| `GET` | `/api/users` | Admin | Lista usuários do tenant |

---

## ❤️ HEALTHCHECKS

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `GET` | `/health` | Não | Liveness: API viva |
| `GET` | `/health/ready` | Não | Readiness: API + SQL Server + dependências |

---

## 🌐 SIGNALR HUB — Sala de Espera

| Hub | Rota | Roles |
|---|---|---|
| WaitingRoomHub | `/hubs/waiting-room` | Admin, Profissional |

**Eventos emitidos pelo servidor:**
- `ReceiveNotification` — Nova notificação da sala de espera

---

## 📖 SWAGGER / OPENAPI

- Disponível em desenvolvimento: `https://localhost:7001/swagger`
- Não habilitado em produção por padrão.

---

## 🔢 CÓDIGOS DE RESPOSTA PADRÃO

| Status | Situação |
|---|---|
| `200 OK` | Sucesso em GET e operações sem novo recurso |
| `201 Created` | Recurso criado com sucesso |
| `204 No Content` | Operação sem retorno (ex: change-password) |
| `400 Bad Request` | Validação falhou (retorna `ValidationProblemDetails`) |
| `401 Unauthorized` | Token ausente, inválido ou expirado |
| `403 Forbidden` | Role não tem permissão para o recurso |
| `404 Not Found` | Recurso não encontrado ou pertence a outro tenant |
| `409 Conflict` | Conflito de regra de negócio (ex: horário ocupado) |
| `500 Internal Server Error` | Erro interno (mensagem genérica, detalhe em log) |
