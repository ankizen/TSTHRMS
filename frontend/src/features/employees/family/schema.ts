import { z } from "zod"

export const familyFormSchema = z.object({
  relation: z.enum(["Spouse", "Parent", "Child", "Other"]),
  name: z.string().min(1, "Name is required").max(200),
  dateOfBirth: z.string().optional().nullable(),
  isDependent: z.boolean(),
  isDifferentlyAbled: z.boolean(),
})

export type FamilyFormValues = z.infer<typeof familyFormSchema>
