# Docker Desktop - ElloSaúde

Este Compose fornece um ambiente **local de desenvolvimento** com SQL Server, RabbitMQ, API e frontend. Ele não é uma configuração de produção: a API inicia em `Development` e os segredos locais não devem ser reutilizados em outros ambientes.

## Preparar os segredos locais

No PowerShell, a partir da raiz do repositório:

```powershell
Set-Location .\ElloSaudeAPI
Copy-Item .env.example .env
```

Edite `.env` e substitua todos os valores de exemplo por valores aleatórios exclusivos. Use uma senha forte para SQL Server e RabbitMQ e uma chave JWT com pelo menos 32 bytes. Preserve um `.env` existente; ele é ignorado pelo Git e não deve ser compartilhado ou commitado.

## Iniciar e verificar

Ainda em `ElloSaudeAPI`:

```powershell
docker compose config --quiet
docker compose up --build -d
docker compose ps
```

O SQL Server armazena seus dados no volume Docker nomeado `sqlserver_data`. O API aguarda o SQL Server e o RabbitMQ saudáveis; as migrations pendentes são aplicadas pela API durante a inicialização. O frontend aguarda a API ficar saudável.

| Componente | Endereço |
| --- | --- |
| Frontend | <http://localhost:8088> |
| API | <http://localhost:5080> |
| Swagger (ambiente Development) | <http://localhost:5080/swagger> |
| SQL Server | `localhost,1433` |
| RabbitMQ Management | <http://localhost:15672> |

As portas do host são publicadas somente em `127.0.0.1`. Os healthchecks estão disponíveis em `/health` e `/health/ready`; o frontend também expõe `/health`.

O seed local cria `admin@ellosaude.com`. A senha é o valor de `DEVELOPMENT_SEED_PASSWORD` no `.env`; não existe senha padrão no repositório.

## Diagnóstico e encerramento

```powershell
docker compose logs --tail 100 api
docker compose logs --tail 100 sqlserver
docker compose down
```

`docker compose down` para e remove os containers e a rede, preservando o volume do banco. **Não** use `docker compose down -v` para uma parada comum: a opção `-v` remove o volume e apaga os dados locais do SQL Server.

Pagamentos no sistema são registros administrativos da clínica; não há gateway nem processamento financeiro externo.
