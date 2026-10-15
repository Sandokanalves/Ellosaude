# 🏥 ELLO SAÚDE — SaaS de Gestão Clínica

Sistema SaaS multi-tenant para clínicas e consultórios médicos, com agenda inteligente, prontuário eletrônico (LGPD/CFM), receituário com PDF, financeiro completo e portal do paciente.

---

## ✨ Funcionalidades Principais

| Módulo | Descrição |
|---|---|
| 🔐 **Autenticação RBAC** | 4 perfis: Admin, Profissional, Secretaria, Paciente |
| 👥 **Pacientes** | Cadastro completo com alerta de alergias e portal de acesso |
| 📅 **Agenda Inteligente** | Disponibilidade semanal, horários livres, bloqueios, sala de espera em tempo real |
| 📋 **Prontuário Imutável** | SOAP, adendos, auditoria, marcadores anatômicos, anexos de exames |
| 💊 **Receituário** | Medicamentos, posologia, PDF gerado e download seguro |
| 💰 **Financeiro** | Pagamento, retorno gratuito, relatório por profissional e método |
| 🏠 **Portal do Paciente** | Agendamento, cancelamento, receitas — autosserviço seguro |
| 🔔 **Sala de Espera** | Notificações em tempo real via SignalR + RabbitMQ |
| 🏥 **Multi-Tenant** | Isolamento total de dados entre clínicas |

---

## 🚀 Início Rápido (Docker)

**Pré-requisito:** Docker Desktop instalado e em execução.

```powershell
# 1. Clone o repositório
git clone https://github.com/Sandokanalves/Ellosaude.git
cd ClinicaProjeto/ElloSaudeAPI

# 2. Configure as variáveis de ambiente
copy .env.example .env
# Edite o .env com suas senhas

# 3. Suba o ecossistema completo
docker compose up --build -d

# 4. Verifique a saúde
curl http://localhost:7001/health/ready
```

**URLs:**
- 🌐 Frontend: `http://localhost:5173`
- ⚙️ API: `http://localhost:7001`
- 📖 Swagger: `http://localhost:7001/swagger`
- 🐇 RabbitMQ: `http://localhost:15672`

---

## 🔑 Credenciais Iniciais

| Usuário | E-mail | Role |
|---|---|---|
| Admin | `admin@ellosaude.com` | Admin |
| Médico | `doutor@ellosaude.com` | Profissional |
| Secretária | `secretaria@ellosaude.com` | Secretaria |

> Senha: valor da variável `SEED_PASSWORD` no `.env`.

---

## 🧪 Testes

```powershell
cd ElloSaudeAPI
dotnet test ElloSaude.sln
```

**Resultado esperado:** 53 aprovados, 0 falhas, 1 ignorado (RabbitMQ — requer container ativo).

---

## 🏗️ Stack Tecnológica

| Camada | Tecnologia |
|---|---|
| Backend | .NET 9 + ASP.NET Core Web API |
| Arquitetura | Clean Architecture + CQRS (MediatR) |
| Banco de Dados | SQL Server (Docker) + EF Core 9 |
| Mensageria | RabbitMQ + Outbox Pattern |
| PDF | QuestPDF |
| Frontend | Vue 3 + PrimeVue + Pinia |
| Containerização | Docker + Docker Compose |

---

## 📁 Estrutura do Projeto

```
ClinicaProjeto/
├── ElloSaudeAPI/       ← Backend .NET 9
│   ├── ElloSaude.Domain/
│   ├── ElloSaude.Application/
│   ├── ElloSaude.Infrastructure/
│   ├── ElloSaude.Api/
│   ├── ElloSaude.UnitTests/      ← 48 testes unitários
│   └── ElloSaude.IntegrationTests/ ← 6 testes de integração
├── ElloSaudeWeb/       ← Frontend Vue 3
└── docs/               ← Documentação técnica completa
```

---

## 📚 Documentação

| Documento | Descrição |
|---|---|
| [ARQUITETURA.md](docs/ARQUITETURA.md) | Stack, camadas, multi-tenancy, fluxo de dados |
| [API_ENDPOINTS.md](docs/API_ENDPOINTS.md) | Inventário completo de endpoints REST |
| [MODELO_DE_DADOS.md](docs/MODELO_DE_DADOS.md) | Entidades, relacionamentos, enums |
| [SEGURANCA_E_LGPD.md](docs/SEGURANCA_E_LGPD.md) | JWT, RBAC, auditoria, LGPD |
| [REQUISITOS_FUNCIONAIS.md](docs/REQUISITOS_FUNCIONAIS.md) | Matriz de requisitos com status |
| [TESTES.md](docs/TESTES.md) | Cobertura e instruções de teste |
| [DOCKER.md](docs/DOCKER.md) | Guia operacional Docker |
| [DESENVOLVIMENTO.md](docs/DESENVOLVIMENTO.md) | Onboarding para devs |
| [DEPLOY_PRODUCAO.md](docs/DEPLOY_PRODUCAO.md) | Checklist e guia de produção |
| [BANCO_DE_DADOS.md](docs/BANCO_DE_DADOS.md) | Migrations, backup e recuperação |
| [PACOTES_E_DEPENDENCIAS.md](docs/PACOTES_E_DEPENDENCIAS.md) | Dependências e licenças |
| [OPERACAO_E_MONITORAMENTO.md](docs/OPERACAO_E_MONITORAMENTO.md) | Logs, diagnósticos, alertas |
| [CHANGELOG.md](docs/CHANGELOG.md) | Histórico de versões |
| [PLANO_DE_IMPLEMENTACAO.md](docs/PLANO_DE_IMPLEMENTACAO.md) | Plano de evolução do projeto |

---

## 🛡️ Segurança e LGPD

- **Isolamento multi-tenant** via Global Query Filter — dados de uma clínica nunca vazam para outra.
- **Prontuário imutável** — apenas adendos; nenhum dado clínico é apagado.
- **Auditoria completa** — todas as leituras e escritas críticas são registradas com timestamp e IP.
- **RBAC rigoroso** — secretárias não acessam prontuários; pacientes acessam apenas seus próprios dados.
- **JWT seguro** — chave via variável de ambiente, validação de issuer/audience/expiração.

---

## 📄 Licença

Projeto privado — todos os direitos reservados.  
QuestPDF: Community License (verificar elegibilidade comercial antes do lançamento).
