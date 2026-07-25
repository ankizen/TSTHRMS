export type CustomFieldType = "Text" | "Number" | "Date" | "Boolean" | "Select"

export interface CustomFieldDefinition {
  id: string
  name: string
  label: string
  fieldType: CustomFieldType
  options: string[] | null
  isRequired: boolean
  displayOrder: number
}

export interface CustomFieldDefinitionWriteRequest {
  name: string
  label: string
  fieldType: CustomFieldType
  options: string[] | null
  isRequired: boolean
  displayOrder: number
}
