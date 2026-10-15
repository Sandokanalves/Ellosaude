# 🧪 TESTES — RELATÓRIO DE COBERTURA E INSTRUÇÕES

## 1. RESULTADO ATUAL DA SUÍTE

| Projeto | Tipo | Testes | Aprovados | Com Falha | Ignorados |
|---|---|---|---|---|---|
| ElloSaude.UnitTests | Unitários | 48 | **48** | 0 | 0 |
| ElloSaude.IntegrationTests | Integração | 6 | **5** | 0 | 1 (RabbitMQ) |
| **Total** | | **54** | **53** | **0** | **1** |

> O teste ignorado (`OutboxEvent_IsPublishedConsumedPersistedAndPushed`) requer RabbitMQ em execução. Ele passa quando o docker-compose está ativo.

---

## 2. ESTRUTURA DOS TESTES

```
ElloSaudeAPI/
├── ElloSaude.UnitTests/
│   ├── Application/
│   │   ├── AppointmentSchedulingTests.cs        (15 testes)
│   │   ├── AppointmentStatusNotificationTests.cs (5 testes)
│   │   ├── AppointmentsQueryAuthorizationTests.cs (8 testes)
│   │   ├── CreatePatientHandlerTests.cs           (2 testes)
│   │   └── ValidationBehaviorTests.cs             (3 testes)
│   ├── Domain/
│   │   ├── AppointmentTests.cs          (6 testes)
│   │   ├── PatientTests.cs              (2 testes)
│   │   ├── PaymentRecordTests.cs        (4 testes)
│   │   └── PrescriptionPdfTests.cs      (1 teste)
│   └── Infrastructure/                  (3 testes — DbContext)
│
└── ElloSaude.IntegrationTests/
    ├── AuthAndPatientsIntegrationTests.cs  (5 testes)
    ├── MultiTenantIsolationTests.cs        (6 testes — novos)
    └── WaitingRoomRabbitMqIntegrationTests.cs (1 teste — requer RabbitMQ)
```

---

## 3. COBERTURA POR MÓDULO

### 3.1. Agendamento e Disponibilidade
- ✅ Rejeita sobreposição de horário do mesmo médico
- ✅ Rejeita agendamento quando não há disponibilidade configurada
- ✅ Rejeita profissional agendando na agenda de outro profissional
- ✅ Rejeita agendamento fora do horário de atendimento
- ✅ Rejeita consulta duplicada para o mesmo paciente
- ✅ Permite agendamento no horário exato de início e fim
- ✅ Permite secretária criar agendamento
- ✅ Rejeita cancelamento por usuário sem permissão
- ✅ Valida ciclo completo de vida do agendamento
- ✅ Rejeita marcação de "no show" antes do horário previsto
- ✅ Permite "no show" após o horário previsto

### 3.2. Financeiro
- ✅ Pagamento parcial acumula corretamente
- ✅ Rejeita combinação de formas de pagamento diferentes
- ✅ Rejeita marcação como retorno gratuito quando já há pagamento
- ✅ Cancelamento exige estorno quando há pagamento recebido

### 3.3. Validação e Segurança
- ✅ ValidationBehavior rejeita request inválido antes de executar handler
- ✅ Secretária recebe 403 ao acessar prontuários
- ✅ Secretária recebe 403 ao acessar relatório financeiro detalhado
- ✅ Secretária pode acessar pagamentos pendentes (200 OK)
- ✅ Profissional recebe 403 no hub SignalR com role incorreta
- ✅ Notificações restritas à role Profissional

### 3.4. Portal do Paciente
- ✅ Troca obrigatória de senha temporária antes de acessar portal
- ✅ Paciente não acessa agendamentos de outros pacientes
- ✅ Paciente não acessa receitas de outros pacientes
- ✅ Cancelamento com senha antiga é rejeitado após troca

### 3.5. Isolamento Multi-Tenant
- ✅ Global Query Filter isola pacientes por tenant
- ✅ ID de paciente estrangeiro retorna 404
- ✅ Paciente do portal não vê receitas de outros pacientes
- ✅ Agendamento estrangeiro retorna 404
- ✅ Paciente não cancela agendamentos alheios

### 3.6. PDF de Receita
- ✅ Geração de PDF retorna bytes com header `%PDF-`

---

## 4. COMO EXECUTAR OS TESTES

### 4.1. Todos os testes (sem banco externo)
```powershell
cd ElloSaudeAPI
dotnet test ElloSaude.sln
```

### 4.2. Apenas testes unitários
```powershell
dotnet test ElloSaude.UnitTests/ElloSaude.UnitTests.csproj
```

### 4.3. Apenas testes de integração
```powershell
dotnet test ElloSaude.IntegrationTests/ElloSaude.IntegrationTests.csproj
```

### 4.4. Com saída detalhada
```powershell
dotnet test ElloSaude.sln --logger "console;verbosity=detailed"
```

### 4.5. Teste do RabbitMQ (requer docker-compose ativo)
```powershell
# 1. Suba o RabbitMQ
docker compose up rabbitmq -d

# 2. Execute todos os testes (incluindo o de integração RabbitMQ)
dotnet test ElloSaude.IntegrationTests/ElloSaude.IntegrationTests.csproj
```

---

## 5. DEPENDÊNCIAS DOS TESTES

| Pacote | Finalidade |
|---|---|
| `xUnit` | Framework de testes |
| `FluentAssertions` | Asserções fluentes e legíveis |
| `Moq` | Mocking de interfaces |
| `Microsoft.AspNetCore.Mvc.Testing` | WebApplicationFactory para testes de integração |
| `Microsoft.EntityFrameworkCore.InMemory` | Banco de dados em memória para testes |

---

## 6. PROTOCOLO DE ADIÇÃO DE NOVOS TESTES

Ao implementar um novo caso de uso, o desenvolvedor DEVE:

1. Criar ao menos um **teste unitário** cobrindo o "caminho feliz".
2. Criar ao menos um **teste unitário** cobrindo a regra de negócio mais crítica.
3. Se o caso de uso envolve autenticação ou isolamento de dados, criar um **teste de integração**.
4. Executar `dotnet test` e garantir que **zero testes falhem** antes do commit.

---

## 7. PENDÊNCIAS DE COBERTURA (FUTURO)

- [ ] Teste de imutabilidade via API (tentativa de PUT em prontuário via HTTP → 405)
- [ ] Teste de geração de PDF com timeout/falha do QuestPDF
- [ ] Teste de estorno financeiro (quando implementado)
- [ ] Teste de assinatura digital de receita (quando implementado)
- [ ] Teste de dois tenants reais no mesmo banco SQL Server (teste de integração com banco compartilhado)
