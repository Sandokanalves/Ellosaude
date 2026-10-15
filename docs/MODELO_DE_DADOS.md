# 🗃️ MODELO DE DADOS — ELLO SAÚDE

## 1. ENTIDADES PRINCIPAIS

### BaseEntity (base de todas as entidades)
```
BaseEntity {
  Id         : Guid        (PK, auto-gerado)
  CreatedAt  : DateTime    (UTC, preenchido automaticamente)
  UpdatedAt  : DateTime    (UTC, atualizado automaticamente)
  IsDeleted  : bool        (soft delete, default false)
}
```

---

### Patient (Paciente)
```
Patient : BaseEntity {
  Name              : string       (obrigatório)
  SocialName        : string?      (nome social / preferido)
  Email             : string       (único por tenant)
  Cpf               : string?      (CPF)
  BirthDate         : DateTime
  Phone             : string?      (telefone / WhatsApp)
  Gender            : string?      (Masculino / Feminino / Outro)
  Profession        : string?
  Address           : string?
  EmergencyContact  : string?
  MarketingOrigin   : string?      (Como conheceu a clínica)
  BloodType         : string?
  Allergies         : string?      (texto livre, exibido com alerta)
  ContinuousMedications : string?
  ChronicConditions : string?
  VaccinationHistory : string?
  IsActive          : bool
  HasPortalAccount  : bool         (computed, baseado em UserId)
  UserId            : Guid?        (FK → User, vínculo portal)
  TenantId          : string       (isolamento multi-tenant)
  
  → Appointments       : ICollection<Appointment>
  → MedicalRecords     : ICollection<MedicalRecord>
  → Prescriptions      : ICollection<Prescription>
  → PaymentRecords     : ICollection<PaymentRecord>
}
```

---

### Professional (Profissional de Saúde)
```
Professional : BaseEntity {
  Name              : string
  CouncilNumber     : string       (CRM, CRO, etc.)
  CouncilState      : string       (UF do conselho)
  Specialty         : string
  Phone             : string?
  UserId            : Guid?        (FK → User)
  IsActive          : bool
  TenantId          : string
  
  → Availabilities  : ICollection<DoctorAvailability>
  → ScheduleBlocks  : ICollection<ScheduleBlock>
  → Appointments    : ICollection<Appointment>
}
```

---

### Appointment (Agendamento)
```
Appointment : BaseEntity {
  Start             : DateTime     (UTC)
  End               : DateTime     (UTC)
  PatientId         : Guid         (FK → Patient)
  ProfessionalId    : Guid         (FK → Professional)
  Type              : AppointmentType  (Consulta | Retorno | Exame | Procedimento)
  Status            : AppointmentStatus
  Price             : decimal?     (valor previsto)
  IsFreeReturn      : bool
  Notes             : string?      (observações)
  CancellationReason: string?
  ArrivedAt         : DateTime?    (chegada na recepção)
  ServiceStartedAt  : DateTime?    (início do atendimento)
  WhatsAppConfirmationSentAt : DateTime?
  TenantId          : string
  
  → StatusHistory   : ICollection<AppointmentStatusHistory>
  → MedicalRecord   : MedicalRecord?
  → PaymentRecord   : PaymentRecord?
}
```

**Enum AppointmentStatus:**
`Pendente | Confirmado | AguardandoRecepcao | EmAtendimento | Realizado | Cancelado | Faltou`

**Enum AppointmentType:**
`Consulta | Retorno | Exame | Procedimento`

---

### AppointmentStatusHistory (Histórico Imutável de Status)
```
AppointmentStatusHistory : BaseEntity {
  AppointmentId  : Guid
  PreviousStatus : AppointmentStatus
  NewStatus      : AppointmentStatus
  ChangedAt      : DateTime
  ChangedByUserId: Guid
  Notes          : string?
  TenantId       : string
}
```

---

### MedicalRecord (Prontuário Médico — Imutável)
```
MedicalRecord : BaseEntity {
  PatientId      : Guid         (FK → Patient)
  ProfessionalId : Guid         (FK → Professional)
  AppointmentId  : Guid?        (FK → Appointment, null apenas em legados)
  SubjectiveNote : string       (S — Subjetivo)
  ObjectiveNote  : string       (O — Objetivo)
  AssessmentNote : string       (A — Avaliação)
  PlanNote       : string       (P — Plano)
  IsLegacy       : bool         (registros anteriores ao sistema)
  SignedAt        : DateTime    (assinado no momento da criação)
  SignedByUserId  : Guid
  TenantId        : string
  
  → Addenda      : ICollection<MedicalRecordAddendum>
}
```

### MedicalRecordAddendum (Adendo — Sem Sobrescrita)
```
MedicalRecordAddendum : BaseEntity {
  MedicalRecordId : Guid
  Content         : string      (texto do adendo)
  AddedByUserId   : Guid
  AddedAt         : DateTime
  TenantId        : string
}
```

---

### Prescription (Receita Médica)
```
Prescription : BaseEntity {
  PatientId       : Guid
  ProfessionalId  : Guid
  AppointmentId   : Guid?
  IssuedAt        : DateTime
  GeneralInstructions : string?
  Status          : PrescriptionStatus  (Rascunho | Finalizada | Cancelada)
  PdfHash         : string?             (SHA-256 do PDF gerado)
  TenantId        : string
  
  → Items         : ICollection<PrescriptionItem>
}
```

### PrescriptionItem (Item da Receita)
```
PrescriptionItem : BaseEntity {
  PrescriptionId  : Guid
  MedicationName  : string
  Dosage          : string       (concentração)
  Frequency       : string       (posologia)
  Duration        : string?
  Route           : string?      (via de administração)
  Notes           : string?
  TenantId        : string
}
```

---

### PaymentRecord (Registro Financeiro)
```
PaymentRecord : BaseEntity {
  AppointmentId   : Guid
  PatientId       : Guid
  ProfessionalId  : Guid
  ExpectedAmount  : decimal      (valor previsto)
  AmountPaid      : decimal      (valor acumulado pago)
  PaymentMethod   : PaymentMethod?
  Status          : PaymentStatus
  PaidAt          : DateTime?
  CancellationReason : string?
  Notes           : string?
  RegisteredByUserId : Guid?
  TenantId        : string
}
```

**Enum PaymentStatus:**
`Pendente | ParcialmentePago | Pago | Cancelado | RetornoGratuito`

**Enum PaymentMethod:**
`Pix | Dinheiro | CartaoDebito | CartaoCredito | Transferencia`

---

### DoctorAvailability (Disponibilidade Semanal)
```
DoctorAvailability : BaseEntity {
  ProfessionalId     : Guid
  DayOfWeek          : DayOfWeek    (0=Dom a 6=Sab)
  StartTime          : TimeSpan
  EndTime            : TimeSpan
  SlotDurationMinutes: int          (duração de cada consulta)
  LunchBreakStart    : TimeSpan?
  LunchBreakEnd      : TimeSpan?
  TenantId           : string
}
```

### ScheduleBlock (Bloqueio de Agenda)
```
ScheduleBlock : BaseEntity {
  ProfessionalId : Guid
  Start          : DateTime
  End            : DateTime
  Reason         : string?
  TenantId       : string
}
```

---

### Clinic (Clínica / Tenant)
```
Clinic : BaseEntity {
  Name                       : string
  TenantId                   : string      (identificador único do tenant)
  FantasyName                : string?
  Cnpj                       : string?
  Phone                      : string?
  Email                      : string?
  Address                    : string?
  City                       : string?
  State                      : string?
  LogoBase64                 : string?
  MaxReturnDays              : int         (dias para retorno gratuito)
  DefaultCommissionPercentage: decimal     (% de repasse médico)
}
```

---

### User (Usuário do Sistema)
```
User : BaseEntity {
  Email           : string
  PasswordHash    : string
  Role            : string   (Admin | Profissional | Secretaria | Paciente)
  TenantId        : string
  MustChangePassword : bool  (portal do paciente: troca na 1ª vez)
  IsActive        : bool
}
```

---

### AuditLog (Auditoria LGPD)
```
AuditLog {
  Id          : Guid
  TenantId    : string
  UserId      : Guid
  Action      : string    (Create | Read | Update | AddAddendum | DownloadPdf)
  EntityName  : string
  EntityId    : string
  Timestamp   : DateTime  (UTC)
  IpAddress   : string?
}
```

---

## 2. DIAGRAMA DE RELACIONAMENTOS

```
Clinic (1) ──────────────── (N) Patient
Clinic (1) ──────────────── (N) Professional
Clinic (1) ──────────────── (N) User

Patient    (1) ─── (N) Appointment
Professional(1)─── (N) Appointment
Appointment(1) ─── (1) MedicalRecord         [opcional]
Appointment(1) ─── (1) PaymentRecord         [opcional]
Appointment(1) ─── (N) AppointmentStatusHistory
MedicalRecord(1)── (N) MedicalRecordAddendum
Patient    (1) ─── (N) Prescription
Prescription(1)─── (N) PrescriptionItem
Professional(1)─── (N) DoctorAvailability
Professional(1)─── (N) ScheduleBlock
User       (1) ─── (0..1) Patient            [portal: Patient.UserId]
User       (1) ─── (0..1) Professional       [login profissional]
```

---

## 3. MIGRATIONS APLICADAS

| Migration | Data | Descrição |
|---|---|---|
| `20260423164609_InitialCreate` | Abr/2026 | Schema inicial: User, Patient, Appointment, MedicalRecord, Clinic |
| `20260504172915_AddProfessionalIdToAppointment` | Mai/2026 | Vincula profissional ao agendamento |
| `20261009010306_AddCompleteSaasClinicalSchema` | Out/2026 | Adiciona Professional, DoctorAvailability, ScheduleBlock, Prescription, PrescriptionItem, PaymentRecord, AuditLog, OutboxMessage, AppointmentStatusHistory, ClinicalAttachment, ClinicalMarker, InAppNotification |
| `20261009022731_AddPatientPortalOwnershipAndAppointmentBoundRecords` | Out/2026 | Vincula Patient.UserId e adiciona constraint de prontuário |
