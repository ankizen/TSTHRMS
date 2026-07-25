import { z } from "zod"

const optionalText = z.string().optional().nullable()

export const employeeFormSchema = z.object({
  legalEntityId: z.string().min(1, "Legal entity is required"),
  productId: z.string().min(1, "Product is required"),
  firstName: z.string().min(1, "First name is required").max(100),
  lastName: z.string().min(1, "Last name is required").max(100),
  gender: z.enum(["Male", "Female", "Other", "PreferNotToSay"]),
  dateOfBirth: optionalText,
  personalEmail: z
    .union([z.literal(""), z.string().email("Enter a valid email address")])
    .optional()
    .nullable(),
  personalPhone: optionalText,
  currentAddress: optionalText,
  permanentAddress: optionalText,
  emergencyContactName: optionalText,
  emergencyContactRelation: optionalText,
  emergencyContactPhone: optionalText,
  bankAccountNumber: optionalText,
  bankIfscCode: z
    .union([
      z.literal(""),
      z.string().regex(/^[A-Za-z]{4}0[A-Za-z0-9]{6}$/, "IFSC code must be 11 characters, e.g. HDFC0001234"),
    ])
    .optional()
    .nullable(),
  dateOfJoining: z.string().min(1, "Date of joining is required"),
  designation: optionalText,
  grade: optionalText,
  department: optionalText,
  reportingManagerId: optionalText,
  employmentType: z.enum(["FullTime", "Contract", "Intern"]),
})

export type EmployeeFormValues = z.infer<typeof employeeFormSchema>
