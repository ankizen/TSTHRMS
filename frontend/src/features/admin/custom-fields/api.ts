import { apiClient } from "@/lib/api-client"
import type { CustomFieldDefinition, CustomFieldDefinitionWriteRequest } from "./types"

export async function getCustomFieldDefinitions(): Promise<CustomFieldDefinition[]> {
  const { data } = await apiClient.get<CustomFieldDefinition[]>("/custom-field-definitions")
  return data
}

export async function createCustomFieldDefinition(
  request: CustomFieldDefinitionWriteRequest,
): Promise<CustomFieldDefinition> {
  const { data } = await apiClient.post<CustomFieldDefinition>("/custom-field-definitions", request)
  return data
}

export async function updateCustomFieldDefinition(
  id: string,
  request: CustomFieldDefinitionWriteRequest,
): Promise<CustomFieldDefinition> {
  const { data } = await apiClient.put<CustomFieldDefinition>(`/custom-field-definitions/${id}`, request)
  return data
}

export async function deleteCustomFieldDefinition(id: string): Promise<void> {
  await apiClient.delete(`/custom-field-definitions/${id}`)
}
