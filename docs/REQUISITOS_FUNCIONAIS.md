# 📋 REQUISITOS FUNCIONAIS — ELLO SAÚDE

## RF01 — AUTENTICAÇÃO E CONTROLE DE ACESSO

| ID | Requisito | Status |
|---|---|:---:|
| RF01.1 | Login com e-mail e senha; token JWT com expiração de 8 horas | ✅ |
| RF01.2 | Quatro perfis de acesso: Admin, Profissional, Secretaria, Paciente | ✅ |
| RF01.3 | Troca obrigatória de senha temporária no primeiro login (portal do paciente) | ✅ |
| RF01.4 | Endpoint de troca de senha autenticado | ✅ |
| RF01.5 | Isolamento completo de dados por tenant (clínica) em todas as operações | ✅ |

---

## RF02 — GESTÃO DE PACIENTES

| ID | Requisito | Status |
|---|---|:---:|
| RF02.1 | Cadastro completo: nome, nome social, CPF, data de nascimento, telefone, e-mail, gênero, endereço, profissão, contato de emergência, origem de marketing | ✅ |
| RF02.2 | Listagem com busca por nome e CPF, paginada | ✅ |
| RF02.3 | Edição de dados cadastrais sem perda de histórico clínico | ✅ |
| RF02.4 | Desativação do paciente (soft delete) | ✅ |
| RF02.5 | Criação de conta no portal do paciente (vincula `Patient.UserId`) | ✅ |
| RF02.6 | Exibição de alerta visual de alergias em destaque | ✅ |

---

## RF03 — PRONTUÁRIO ELETRÔNICO (CFM)

| ID | Requisito | Status |
|---|---|:---:|
| RF03.1 | Registro clínico vinculado obrigatoriamente a uma consulta realizada | ✅ |
| RF03.2 | Estrutura SOAP: Subjetivo, Objetivo, Avaliação, Plano | ✅ |
| RF03.3 | Imutabilidade: prontuário não pode ser editado ou excluído | ✅ |
| RF03.4 | Adendos: correções e observações posteriores sem sobrescrita | ✅ |
| RF03.5 | Acesso restrito apenas a profissionais de saúde | ✅ |
| RF03.6 | Auditoria: log de criação e leitura de prontuário | ✅ |
| RF03.7 | Marcadores anatômicos visuais em diagrama corporal | ✅ |
| RF03.8 | Anexos de exames (imagens, documentos) | ✅ |
| RF03.9 | Resumo clínico: alergias, medicações contínuas, condições crônicas, tipo sanguíneo, vacinas | ✅ |

---

## RF04 — AGENDA E DISPONIBILIDADE

| ID | Requisito | Status |
|---|---|:---:|
| RF04.1 | Configuração de disponibilidade semanal por profissional (dias, horário, duração de slot) | ✅ |
| RF04.2 | Consulta de horários livres por profissional e data | ✅ |
| RF04.3 | Bloqueios temporários de agenda (férias, eventos) | ✅ |
| RF04.4 | Impedimento de agendamento fora do horário configurado | ✅ |
| RF04.5 | Impedimento de conflito de horário para o mesmo profissional | ✅ |
| RF04.6 | Impedimento de agendamento duplicado para o mesmo paciente | ✅ |
| RF04.7 | Ciclo de vida do agendamento: Pendente → Confirmado → AguardandoRecepcao → EmAtendimento → Realizado / Cancelado / Faltou | ✅ |
| RF04.8 | Histórico imutável de transições de status | ✅ |
| RF04.9 | Confirmação de consulta via WhatsApp (link) | ✅ |
| RF04.10 | Sala de espera virtual com notificações em tempo real (SignalR + RabbitMQ) | ✅ |
| RF04.11 | Filtro da agenda por profissional | ✅ |

---

## RF05 — RECEITUÁRIO ELETRÔNICO

| ID | Requisito | Status |
|---|---|:---:|
| RF05.1 | Emissão de receita vinculada a consulta realizada e restrita ao profissional emissor | ✅ |
| RF05.2 | Itens de medicamento: nome, concentração, posologia, duração, via de administração | ✅ |
| RF05.3 | Geração de PDF da receita (QuestPDF) | ✅ |
| RF05.4 | Download do PDF restrito ao profissional emissor e ao paciente (portal) | ✅ |
| RF05.5 | Receita liberada para o paciente no portal após emissão | ✅ |
| RF05.6 | Validação de licença comercial QuestPDF antes de produção | ⚠️ Pendente |
| RF05.7 | Assinatura digital válida (ICP-Brasil) | ⚠️ Futuro |

---

## RF06 — FINANCEIRO

| ID | Requisito | Status |
|---|---|:---:|
| RF06.1 | Registro de pagamento com forma: Pix, Dinheiro, Cartão Débito, Cartão Crédito, Transferência | ✅ |
| RF06.2 | Pagamento parcial na mesma forma de pagamento | ✅ |
| RF06.3 | Status financeiro: Pendente, Parcialmente Pago, Pago, Cancelado, Retorno Gratuito | ✅ |
| RF06.4 | Marcação de retorno gratuito (zera valor previsto) | ✅ |
| RF06.5 | Cancelamento de cobrança (somente sem pagamento recebido) | ✅ |
| RF06.6 | Relatório financeiro por período, profissional e forma de pagamento | ✅ |
| RF06.7 | Total recebido, saldo pendente e contagem de retornos | ✅ |
| RF06.8 | Estorno de valor já recebido | ⚠️ Pendente |

---

## RF07 — PORTAL DO PACIENTE

| ID | Requisito | Status |
|---|---|:---:|
| RF07.1 | Visualização de próximos agendamentos e histórico | ✅ |
| RF07.2 | Agendamento de consulta pelo portal (horários livres disponíveis) | ✅ |
| RF07.3 | Cancelamento de consulta pelo portal (mínimo 24h de antecedência) | ✅ |
| RF07.4 | Listagem e download de receitas liberadas | ✅ |
| RF07.5 | Acesso restrito ao paciente autenticado (sem acesso a dados alheios) | ✅ |

---

## RF08 — CONFIGURAÇÕES DA CLÍNICA

| ID | Requisito | Status |
|---|---|:---:|
| RF08.1 | Dados cadastrais da clínica: razão social, CNPJ, telefone, endereço | ✅ |
| RF08.2 | Upload e exibição do logotipo da clínica | ✅ |
| RF08.3 | Prazo máximo de retorno gratuito (dias configurável) | ✅ |
| RF08.4 | Comissão médica padrão (%) | ✅ |
| RF08.5 | Tabela de preços por procedimento | ✅ |
| RF08.6 | Gestão de profissionais (cadastro de CRM, especialidade, horários) | ✅ |
| RF08.7 | Gestão de usuários da plataforma | ✅ |

---

## RF09 — INFRAESTRUTURA E OPERAÇÃO

| ID | Requisito | Status |
|---|---|:---:|
| RF09.1 | API containerizada via Docker | ✅ |
| RF09.2 | Frontend servido por Nginx em container Docker | ✅ |
| RF09.3 | SQL Server em container com volume persistente | ✅ |
| RF09.4 | RabbitMQ em container com volume persistente | ✅ |
| RF09.5 | Healthchecks: `/health` e `/health/ready` | ✅ |
| RF09.6 | Migrations aplicadas automaticamente na inicialização | ✅ |
| RF09.7 | Seed de dados iniciais (admin, médico, secretária) | ✅ |
| RF09.8 | Variáveis de ambiente documentadas em `.env.example` | ✅ |
