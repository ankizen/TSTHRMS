import { z } from "zod"

export const identityDocumentFormSchema = z
  .object({
    documentType: z.enum(["Pan", "Aadhaar", "Passport", "Uan", "Esic"]),
    number: z.string().min(1, "Number is required"),
    expiryDate: z.string().optional().nullable(),
  })
  .superRefine((values, ctx) => {
    if (values.documentType === "Pan" && !/^[A-Za-z]{5}[0-9]{4}[A-Za-z]$/.test(values.number)) {
      ctx.addIssue({
        code: "custom",
        path: ["number"],
        message: "PAN must be 10 characters in the format ABCDE1234F.",
      })
    }

    if (values.documentType === "Aadhaar" && !/^\d{12}$/.test(values.number)) {
      ctx.addIssue({
        code: "custom",
        path: ["number"],
        message: "Aadhaar number must be exactly 12 digits.",
      })
    }

    if (values.documentType === "Passport" && !values.expiryDate) {
      ctx.addIssue({
        code: "custom",
        path: ["expiryDate"],
        message: "Expiry date is required for a passport.",
      })
    }
  })

export type IdentityDocumentFormValues = z.infer<typeof identityDocumentFormSchema>
