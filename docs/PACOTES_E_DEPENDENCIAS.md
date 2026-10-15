# 📦 PACOTES E DEPENDÊNCIAS — ELLO SAÚDE

## 1. BACKEND (.NET 9)

### ElloSaude.Domain
| Pacote | Versão | Finalidade | Licença |
|---|---|---|---|
| — | — | Sem dependências externas | — |

### ElloSaude.Application
| Pacote | Versão | Finalidade | Licença |
|---|---|---|---|
| `MediatR` | 12.x | CQRS: roteamento de Commands e Queries | Apache 2.0 |
| `FluentValidation.DependencyInjectionExtensions` | 11.x | Validação de entrada com registro automático | Apache 2.0 |

### ElloSaude.Infrastructure
| Pacote | Versão | Finalidade | Licença |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.x | ORM com SQL Server | MIT |
| `Microsoft.EntityFrameworkCore.Tools` | 9.x | CLI para migrations | MIT |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.x | Autenticação JWT | MIT |
| `Microsoft.AspNetCore.Cryptography.KeyDerivation` | 9.x | Hash de senhas (PBKDF2) | MIT |
| `RabbitMQ.Client` | 6.x | Mensageria com RabbitMQ | Apache 2.0 |
| `QuestPDF` | 2024.x | Geração de PDF de receitas | Community (gratuito para projetos open-source e receita < $1M/ano) |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | 9.x | Health check do banco via EF | MIT |

> ⚠️ **QuestPDF**: A licença Community é gratuita para projetos open-source ou receita anual inferior a USD 1.000.000. Verifique e adquira a licença adequada antes do lançamento comercial.

### ElloSaude.Api
| Pacote | Versão | Finalidade | Licença |
|---|---|---|---|
| `Swashbuckle.AspNetCore` | 6.x | Swagger/OpenAPI para documentação da API | MIT |
| `Microsoft.AspNetCore.SignalR` | 9.x | WebSocket para sala de espera | MIT |

### ElloSaude.UnitTests
| Pacote | Versão | Finalidade | Licença |
|---|---|---|---|
| `xunit` | 2.x | Framework de testes | Apache 2.0 |
| `FluentAssertions` | 6.x | Asserções fluentes e legíveis | Apache 2.0 |
| `Moq` | 4.x | Mocking de interfaces | BSD 3-Clause |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.x | Banco InMemory para testes | MIT |

### ElloSaude.IntegrationTests
| Pacote | Versão | Finalidade | Licença |
|---|---|---|---|
| `Microsoft.AspNetCore.Mvc.Testing` | 9.x | WebApplicationFactory para testes HTTP | MIT |
| `xunit` | 2.x | Framework de testes | Apache 2.0 |
| `FluentAssertions` | 6.x | Asserções | Apache 2.0 |

---

## 2. FRONTEND (Node.js / Vue 3)

| Pacote | Versão | Finalidade | Licença |
|---|---|---|---|
| `vue` | 3.x | Framework principal | MIT |
| `vite` | 5.x | Build tool e dev server | MIT |
| `@vitejs/plugin-vue` | 5.x | Plugin Vite para Vue SFC | MIT |
| `vue-router` | 4.x | Roteamento client-side | MIT |
| `pinia` | 2.x | Gerenciamento de estado | MIT |
| `primevue` | 4.x | Biblioteca de componentes UI | MIT |
| `primeicons` | 7.x | Ícones PrimeVue | MIT |
| `axios` | 1.x | HTTP client | MIT |
| `chart.js` | 4.x | Gráficos financeiros | MIT |
| `@vue-chartjs` | 5.x | Wrapper Vue para Chart.js | MIT |

---

## 3. INFRAESTRUTURA (DOCKER)

| Imagem | Tag | Finalidade | Licença |
|---|---|---|---|
| `mcr.microsoft.com/mssql/server` | 2022-latest | SQL Server | Microsoft EULA (gratuito para dev) |
| `rabbitmq` | 3-management | Message broker | Mozilla Public License 2.0 |
| `node` | 20-alpine | Build do frontend | MIT |
| `nginx` | alpine | Serve o SPA Vue em produção | 2-clause BSD |
| `mcr.microsoft.com/dotnet/sdk` | 9.0 | Build da API | MIT |
| `mcr.microsoft.com/dotnet/aspnet` | 9.0 | Runtime da API | MIT |

---

## 4. PENDÊNCIAS DE LICENCIAMENTO

| Item | Situação | Ação Necessária |
|---|---|---|
| **QuestPDF** | License Community (gratuita até $1M/ano) | Verificar receita anual; adquirir licença Professional se necessário |
| **SQL Server** | Developer Edition (apenas dev/test) | Adquirir licença Express/Standard para produção com dados reais |

---

## 5. AUDITORIA DE DEPENDÊNCIAS

Para verificar vulnerabilidades nos pacotes .NET:
```powershell
dotnet list package --vulnerable
```

Para verificar pacotes npm desatualizados:
```powershell
npm --prefix ElloSaudeWeb audit
npm --prefix ElloSaudeWeb outdated
```
