import { z } from "zod"

export const educationFormSchema = z.object({
  qualificationLevel: z.enum([
    "TenthOrBelow",
    "TwelfthOrDiploma",
    "Graduate",
    "PostGraduate",
    "Doctorate",
    "Other",
  ]),
  degreeName: z.string().min(1, "Degree/course name is required").max(200),
  instituteName: z.string().min(1, "Institute is required").max(200),
  yearOfPassing: z
    .number()
    .int()
    .min(1950, "Enter a valid year")
    .max(new Date().getFullYear(), "Year of passing cannot be in the future"),
  specialization: z.string().optional().nullable(),
})

export type EducationFormValues = z.infer<typeof educationFormSchema>
