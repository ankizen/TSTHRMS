import { z } from "zod"

export const nomineeFormSchema = z.object({
  nominationType: z.enum(["ProvidentFund", "Gratuity", "Insurance"]),
  name: z.string().min(1, "Name is required").max(200),
  relation: z.string().min(1, "Relation is required").max(100),
  sharePercentage: z
    .number()
    .min(0.01, "Must be greater than 0")
    .max(100, "Can't exceed 100")
    .optional()
    .nullable(),
  contactNumber: z.string().optional().nullable(),
  familyMemberId: z.string().optional().nullable(),
})

export type NomineeFormValues = z.infer<typeof nomineeFormSchema>
