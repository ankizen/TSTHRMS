import { zodResolver } from "@hookform/resolvers/zod"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Eye } from "lucide-react"
import { useEffect, useState } from "react"
import { Controller, useForm } from "react-hook-form"
import { useNavigate, useParams } from "react-router-dom"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import { AuditLogSection } from "./audit-log/audit-log-section"
import { CustomFieldsSection } from "./custom-fields/custom-fields-section"
import { DocumentRepositorySection } from "./documents/document-repository-section"
import { EducationSection } from "./education/education-section"
import { FamilySection } from "./family/family-section"
import { IdentityDocumentSection } from "./identity-documents/identity-document-section"
import { NomineeSection } from "./nominees/nominee-section"
import { PreviousEmploymentSection } from "./previous-employment/previous-employment-section"
import {
  acknowledgePoshPolicy,
  confirmEmployee,
  createEmployee,
  getEmployee,
  getEmployees,
  getLegalEntities,
  getProducts,
  revealBankAccountNumber,
  updateEmployee,
} from "./api"
import {
  DATE_OF_BIRTH_PROOF_TYPE_OPTIONS,
  EMPLOYMENT_TYPE_OPTIONS,
  GENDER_OPTIONS,
  INDIAN_STATES,
} from "./constants"
import { employeeFormSchema, type EmployeeFormValues } from "./schema"
import type { EmployeeWriteRequest } from "./types"

const emptyValues: EmployeeFormValues = {
  legalEntityId: "",
  productId: "",
  firstName: "",
  lastName: "",
  gender: "PreferNotToSay",
  dateOfBirth: "",
  personalEmail: "",
  personalPhone: "",
  currentAddress: "",
  permanentAddress: "",
  emergencyContactName: "",
  emergencyContactRelation: "",
  emergencyContactPhone: "",
  bankAccountNumber: "",
  bankIfscCode: "",
  dateOfJoining: "",
  designation: "",
  grade: "",
  department: "",
  workLocation: "",
  reportingManagerId: "",
  employmentType: "FullTime",
  monthlyGrossSalary: null,
  dateOfBirthProofType: null,
  professionalTaxState: "",
  probationEndDate: "",
  contractStartDate: "",
  contractEndDate: "",
}

export function EmployeeFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [revealedBankAccount, setRevealedBankAccount] = useState<string | null>(null)
  const [confirmingManagerId, setConfirmingManagerId] = useState("")

  const employeeQuery = useQuery({
    queryKey: ["employee", id],
    queryFn: () => getEmployee(id!),
    enabled: isEdit,
  })

  const legalEntitiesQuery = useQuery({ queryKey: ["legal-entities"], queryFn: getLegalEntities })
  const productsQuery = useQuery({ queryKey: ["products"], queryFn: getProducts })
  const managersQuery = useQuery({
    queryKey: ["employees", "managers"],
    queryFn: () => getEmployees({ page: 1, pageSize: 200, status: "Active" }),
  })

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<EmployeeFormValues>({
    resolver: zodResolver(employeeFormSchema),
    defaultValues: emptyValues,
  })

  useEffect(() => {
    const employee = employeeQuery.data
    if (!employee) return

    reset({
      legalEntityId: employee.legalEntityId,
      productId: employee.productId,
      firstName: employee.firstName,
      lastName: employee.lastName,
      gender: employee.gender,
      dateOfBirth: employee.dateOfBirth ?? "",
      personalEmail: employee.personalEmail ?? "",
      personalPhone: employee.personalPhone ?? "",
      currentAddress: employee.currentAddress ?? "",
      permanentAddress: employee.permanentAddress ?? "",
      emergencyContactName: employee.emergencyContactName ?? "",
      emergencyContactRelation: employee.emergencyContactRelation ?? "",
      emergencyContactPhone: employee.emergencyContactPhone ?? "",
      bankAccountNumber: "",
      bankIfscCode: employee.bankIfscCode ?? "",
      dateOfJoining: employee.dateOfJoining,
      designation: employee.designation ?? "",
      grade: employee.grade ?? "",
      department: employee.department ?? "",
      workLocation: employee.workLocation ?? "",
      reportingManagerId: employee.reportingManagerId ?? "",
      employmentType: employee.employmentType,
      monthlyGrossSalary: employee.monthlyGrossSalary,
      dateOfBirthProofType: employee.dateOfBirthProofType,
      professionalTaxState: employee.professionalTaxState ?? "",
      probationEndDate: employee.probationEndDate ?? "",
      contractStartDate: employee.contractStartDate ?? "",
      contractEndDate: employee.contractEndDate ?? "",
    })
  }, [employeeQuery.data, reset])

  const saveMutation = useMutation({
    mutationFn: (values: EmployeeFormValues) => {
      const request: EmployeeWriteRequest = {
        ...values,
        dateOfBirth: values.dateOfBirth || null,
        personalEmail: values.personalEmail || null,
        personalPhone: values.personalPhone || null,
        currentAddress: values.currentAddress || null,
        permanentAddress: values.permanentAddress || null,
        emergencyContactName: values.emergencyContactName || null,
        emergencyContactRelation: values.emergencyContactRelation || null,
        emergencyContactPhone: values.emergencyContactPhone || null,
        bankAccountNumber: values.bankAccountNumber || null,
        bankIfscCode: values.bankIfscCode || null,
        designation: values.designation || null,
        grade: values.grade || null,
        department: values.department || null,
        workLocation: values.workLocation || null,
        reportingManagerId: values.reportingManagerId || null,
        monthlyGrossSalary: values.monthlyGrossSalary ?? null,
        dateOfBirthProofType: values.dateOfBirthProofType || null,
        professionalTaxState: values.professionalTaxState || null,
        probationEndDate: values.probationEndDate || null,
        contractStartDate: values.contractStartDate || null,
        contractEndDate: values.contractEndDate || null,
      }
      return isEdit ? updateEmployee(id!, request) : createEmployee(request)
    },
    onSuccess: async (employee) => {
      toast.success(isEdit ? "Employee updated" : "Employee created", {
        description: `${employee.firstName} ${employee.lastName} (${employee.employeeCode})`,
      })
      await queryClient.invalidateQueries({ queryKey: ["employees"] })
      navigate(`/employees/${employee.id}`)
    },
    onError: () => {
      toast.error("Couldn't save the employee. Check the form for errors and try again.")
    },
  })

  const revealMutation = useMutation({
    mutationFn: () => revealBankAccountNumber(id!),
    onSuccess: (bankAccountNumber) => setRevealedBankAccount(bankAccountNumber ?? "Not on file"),
    onError: () => toast.error("Couldn't reveal the bank account number."),
  })

  const poshMutation = useMutation({
    mutationFn: () => acknowledgePoshPolicy(id!),
    onSuccess: async () => {
      toast.success("POSH policy acknowledgment recorded")
      await queryClient.invalidateQueries({ queryKey: ["employee", id] })
    },
    onError: () => toast.error("Couldn't record the acknowledgment."),
  })

  const confirmMutation = useMutation({
    mutationFn: (request: { confirmingManagerId: string; confirmationDate: string | null }) =>
      confirmEmployee(id!, request),
    onSuccess: async () => {
      toast.success("Employee confirmed")
      setConfirmingManagerId("")
      await queryClient.invalidateQueries({ queryKey: ["employee", id] })
    },
    onError: () => toast.error("Couldn't confirm the employee."),
  })

  const managerOptions = (managersQuery.data?.items ?? []).filter((employee) => employee.id !== id)

  return (
    <form
      onSubmit={handleSubmit((values) => saveMutation.mutate(values))}
      className="mx-auto flex max-w-3xl flex-col gap-6"
    >
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">
            {isEdit ? `Edit ${employeeQuery.data?.firstName ?? "Employee"}` : "New Employee"}
          </h1>
          {employeeQuery.data && (
            <p className="text-muted-foreground">{employeeQuery.data.employeeCode}</p>
          )}
        </div>
        <Button type="submit" disabled={isSubmitting || saveMutation.isPending}>
          {saveMutation.isPending ? "Saving..." : "Save"}
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Master Record</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-2">
            <Label>Legal Entity</Label>
            <Controller
              control={control}
              name="legalEntityId"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select entity" />
                  </SelectTrigger>
                  <SelectContent>
                    {legalEntitiesQuery.data?.map((entity) => (
                      <SelectItem key={entity.id} value={entity.id}>
                        {entity.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.legalEntityId && (
              <p className="text-sm text-destructive">{errors.legalEntityId.message}</p>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label>Product</Label>
            <Controller
              control={control}
              name="productId"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select product" />
                  </SelectTrigger>
                  <SelectContent>
                    {productsQuery.data?.map((product) => (
                      <SelectItem key={product.id} value={product.id}>
                        {product.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.productId && <p className="text-sm text-destructive">{errors.productId.message}</p>}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Personal</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="firstName">First name</Label>
            <Input id="firstName" {...register("firstName")} />
            {errors.firstName && <p className="text-sm text-destructive">{errors.firstName.message}</p>}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="lastName">Last name</Label>
            <Input id="lastName" {...register("lastName")} />
            {errors.lastName && <p className="text-sm text-destructive">{errors.lastName.message}</p>}
          </div>
          <div className="flex flex-col gap-2">
            <Label>Gender</Label>
            <Controller
              control={control}
              name="gender"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {GENDER_OPTIONS.map((option) => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="dateOfBirth">Date of birth</Label>
            <Input id="dateOfBirth" type="date" {...register("dateOfBirth")} />
            {errors.dateOfBirth && (
              <p className="text-sm text-destructive">{errors.dateOfBirth.message}</p>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Contact</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="personalEmail">Personal email</Label>
            <Input id="personalEmail" type="email" {...register("personalEmail")} />
            {errors.personalEmail && (
              <p className="text-sm text-destructive">{errors.personalEmail.message}</p>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="personalPhone">Personal phone</Label>
            <Input id="personalPhone" {...register("personalPhone")} />
          </div>
          <div className="col-span-2 flex flex-col gap-2">
            <Label htmlFor="currentAddress">Current address</Label>
            <Textarea id="currentAddress" {...register("currentAddress")} />
          </div>
          <div className="col-span-2 flex flex-col gap-2">
            <Label htmlFor="permanentAddress">Permanent address</Label>
            <Textarea id="permanentAddress" {...register("permanentAddress")} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="emergencyContactName">Emergency contact name</Label>
            <Input id="emergencyContactName" {...register("emergencyContactName")} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="emergencyContactRelation">Relation</Label>
            <Input id="emergencyContactRelation" {...register("emergencyContactRelation")} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="emergencyContactPhone">Emergency contact phone</Label>
            <Input id="emergencyContactPhone" {...register("emergencyContactPhone")} />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Employment</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="dateOfJoining">Date of joining</Label>
            <Input id="dateOfJoining" type="date" {...register("dateOfJoining")} />
            {errors.dateOfJoining && (
              <p className="text-sm text-destructive">{errors.dateOfJoining.message}</p>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label>Employment type</Label>
            <Controller
              control={control}
              name="employmentType"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {EMPLOYMENT_TYPE_OPTIONS.map((option) => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="designation">Designation</Label>
            <Input id="designation" {...register("designation")} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="grade">Grade</Label>
            <Input id="grade" {...register("grade")} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="department">Department</Label>
            <Input id="department" {...register("department")} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="workLocation">Work Location</Label>
            <Input id="workLocation" {...register("workLocation")} />
          </div>
          <div className="flex flex-col gap-2">
            <Label>Reporting manager</Label>
            <Controller
              control={control}
              name="reportingManagerId"
              render={({ field }) => (
                <Select value={field.value ?? ""} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue placeholder="None" />
                  </SelectTrigger>
                  <SelectContent>
                    {managerOptions.map((manager) => (
                      <SelectItem key={manager.id} value={manager.id}>
                        {manager.firstName} {manager.lastName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Probation & Contract</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="probationEndDate">Probation end date</Label>
              <Input id="probationEndDate" type="date" {...register("probationEndDate")} />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="contractStartDate">Contract start date</Label>
              <Input id="contractStartDate" type="date" {...register("contractStartDate")} />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="contractEndDate">Contract end date</Label>
              <Input id="contractEndDate" type="date" {...register("contractEndDate")} />
              {errors.contractEndDate && (
                <p className="text-sm text-destructive">{errors.contractEndDate.message}</p>
              )}
            </div>
          </div>

          {isEdit && employeeQuery.data && (
            <div className="flex flex-col gap-3 border-t pt-4">
              <div className="flex flex-wrap items-center gap-2">
                <Badge variant={employeeQuery.data.confirmationStatus === "Confirmed" ? "default" : "secondary"}>
                  {employeeQuery.data.confirmationStatus === "Confirmed"
                    ? `Confirmed ${employeeQuery.data.confirmationDate ?? ""} by ${employeeQuery.data.confirmingManagerName ?? "-"}`
                    : "On probation"}
                </Badge>
                {employeeQuery.data.isContractExpiringSoon && (
                  <Badge variant="destructive">Contract expiring soon</Badge>
                )}
              </div>

              {employeeQuery.data.confirmationStatus === "Probation" && (
                <div className="flex items-end gap-2">
                  <div className="flex flex-col gap-2">
                    <Label>Confirming manager</Label>
                    <Select value={confirmingManagerId} onValueChange={setConfirmingManagerId}>
                      <SelectTrigger className="w-[220px]">
                        <SelectValue placeholder="Select manager" />
                      </SelectTrigger>
                      <SelectContent>
                        {managerOptions.map((manager) => (
                          <SelectItem key={manager.id} value={manager.id}>
                            {manager.firstName} {manager.lastName}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={!confirmingManagerId || confirmMutation.isPending}
                    onClick={() =>
                      confirmMutation.mutate({ confirmingManagerId, confirmationDate: null })
                    }
                  >
                    Confirm employee
                  </Button>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Statutory & Compliance</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="monthlyGrossSalary">Monthly gross salary</Label>
              <Input
                id="monthlyGrossSalary"
                type="number"
                step="0.01"
                {...register("monthlyGrossSalary", { valueAsNumber: true })}
              />
              {errors.monthlyGrossSalary && (
                <p className="text-sm text-destructive">{errors.monthlyGrossSalary.message}</p>
              )}
            </div>
            <div className="flex flex-col gap-2">
              <Label>Date of birth proof</Label>
              <Controller
                control={control}
                name="dateOfBirthProofType"
                render={({ field }) => (
                  <Select value={field.value ?? ""} onValueChange={field.onChange}>
                    <SelectTrigger>
                      <SelectValue placeholder="Not recorded" />
                    </SelectTrigger>
                    <SelectContent>
                      {DATE_OF_BIRTH_PROOF_TYPE_OPTIONS.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label>Professional tax state</Label>
              <Controller
                control={control}
                name="professionalTaxState"
                render={({ field }) => (
                  <Select value={field.value ?? ""} onValueChange={field.onChange}>
                    <SelectTrigger>
                      <SelectValue placeholder="Not set" />
                    </SelectTrigger>
                    <SelectContent>
                      {INDIAN_STATES.map((state) => (
                        <SelectItem key={state} value={state}>
                          {state}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </div>
          </div>

          {isEdit && employeeQuery.data && (
            <div className="flex flex-col gap-3 border-t pt-4">
              <div className="flex flex-wrap gap-2">
                <Badge variant={employeeQuery.data.isPfApplicable ? "default" : "secondary"}>
                  PF {employeeQuery.data.isPfApplicable ? "applicable" : "not applicable"}
                </Badge>
                <Badge variant={employeeQuery.data.isEsicApplicable ? "default" : "secondary"}>
                  ESIC {employeeQuery.data.isEsicApplicable ? "applicable" : "not applicable"}
                </Badge>
                <Badge variant={employeeQuery.data.isMaharashtraLwfEligible ? "default" : "secondary"}>
                  LWF {employeeQuery.data.isMaharashtraLwfEligible ? "eligible" : "not eligible"}
                </Badge>
                {employeeQuery.data.hasMinorOrDifferentlyAbledDependent && (
                  <Badge variant="outline">Has minor/differently-abled dependent</Badge>
                )}
              </div>
              <p className="text-xs text-muted-foreground">
                PF/ESIC/LWF are derived automatically from entity registration, salary, and PT
                state - they aren&apos;t manually toggled. Verify LWF figures against the current
                state notification before relying on them for filings.
              </p>

              <div className="flex items-center gap-2">
                {employeeQuery.data.poshAcknowledgedAt ? (
                  <Badge variant="default">
                    POSH policy acknowledged {new Date(employeeQuery.data.poshAcknowledgedAt).toLocaleDateString()}
                  </Badge>
                ) : (
                  <>
                    <Badge variant="secondary">POSH policy not yet acknowledged</Badge>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => poshMutation.mutate()}
                      disabled={poshMutation.isPending}
                    >
                      Record acknowledgment
                    </Button>
                  </>
                )}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Bank Details</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="bankAccountNumber">
              Bank account number
              {isEdit && employeeQuery.data?.bankAccountNumberMasked && (
                <span className="ml-2 text-muted-foreground">
                  Current: {employeeQuery.data.bankAccountNumberMasked}
                </span>
              )}
            </Label>
            <Input
              id="bankAccountNumber"
              placeholder={isEdit ? "Leave blank to keep current" : undefined}
              {...register("bankAccountNumber")}
            />
            {isEdit && employeeQuery.data?.bankAccountNumberMasked && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="w-fit"
                onClick={() => revealMutation.mutate()}
                disabled={revealMutation.isPending}
              >
                <Eye />
                Reveal
              </Button>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="bankIfscCode">IFSC code</Label>
            <Input id="bankIfscCode" {...register("bankIfscCode")} />
            {errors.bankIfscCode && (
              <p className="text-sm text-destructive">{errors.bankIfscCode.message}</p>
            )}
          </div>
        </CardContent>
      </Card>

      {isEdit && id && <EducationSection employeeId={id} />}
      {isEdit && id && <FamilySection employeeId={id} />}
      {isEdit && id && <PreviousEmploymentSection employeeId={id} />}
      {isEdit && id && <IdentityDocumentSection employeeId={id} />}
      {isEdit && id && <NomineeSection employeeId={id} />}
      {isEdit && id && <DocumentRepositorySection employeeId={id} />}
      {isEdit && id && <CustomFieldsSection employeeId={id} />}
      {isEdit && id && <AuditLogSection employeeId={id} />}

      <Dialog open={revealedBankAccount !== null} onOpenChange={() => setRevealedBankAccount(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Bank account number</DialogTitle>
          </DialogHeader>
          <p className="font-mono text-lg">{revealedBankAccount}</p>
          <p className="text-sm text-muted-foreground">
            This reveal has been recorded in the employee's audit log.
          </p>
        </DialogContent>
      </Dialog>
    </form>
  )
}
