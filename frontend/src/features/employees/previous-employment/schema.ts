import { z } from "zod"

export const previousEmploymentFormSchema = z.object({
  companyName: z.string().min(1, "Company name is required").max(200),
  designation: z.string().optional().nullable(),
  yearsOfExperience: z.number().min(0, "Can't be negative").optional().nullable(),
  dateOfJoining: z.string().min(1, "Date of joining is required"),
  dateOfLeaving: z.string().min(1, "Date of leaving is required"),
  reasonForLeaving: z.string().optional().nullable(),
  previousUan: z.string().optional().nullable(),
})

export type PreviousEmploymentFormValues = z.infer<typeof previousEmploymentFormSchema>
