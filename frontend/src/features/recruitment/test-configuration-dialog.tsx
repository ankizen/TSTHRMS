import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import {
  Dialog,
  DialogContent,
  DialogFooter,
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
import type { AssessmentType, TestConfiguration, TestConfigurationRequest } from "./types"

const ASSESSMENT_TYPE_OPTIONS: { value: AssessmentType; label: string }[] = [
  { value: "MachineCodingTest", label: "Machine / Coding Test" },
  { value: "SkillAssignment", label: "Skill Assignment" },
  { value: "AptitudeTest", label: "Aptitude / Psychometric Test" },
  { value: "CaseStudy", label: "Case Study / Business Round" },
]

interface TestConfigurationDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (request: TestConfigurationRequest) => void
  isSubmitting: boolean
  configuration: TestConfiguration | null
}

const defaultConfig: TestConfigurationRequest = {
  isEnabled: false,
  type: "MachineCodingTest",
  instructions: null,
  timeLimitMinutes: 60,
  responseWindowDays: 5,
  passThreshold: 60,
  retakeCooldownMonths: 6,
}

export function TestConfigurationDialog({
  open, onOpenChange, onSubmit, isSubmitting, configuration,
}: TestConfigurationDialogProps) {
  const [form, setForm] = useState<TestConfigurationRequest>(defaultConfig)

  useEffect(() => {
    if (open) {
      setForm(configuration ?? defaultConfig)
    }
  }, [open, configuration])

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault()
    onSubmit(form)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Assessment configuration</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex items-center gap-2">
            <Checkbox
              id="testEnabled"
              checked={form.isEnabled}
              onCheckedChange={(checked) => setForm((f) => ({ ...f, isEnabled: checked === true }))}
            />
            <Label htmlFor="testEnabled" className="cursor-pointer font-normal">
              Require a test for this role
            </Label>
          </div>

          <div className="flex flex-col gap-2">
            <Label>Test type</Label>
            <Select value={form.type} onValueChange={(value) => setForm((f) => ({ ...f, type: value as AssessmentType }))}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {ASSESSMENT_TYPE_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="testInstructions">Instructions shown to the candidate</Label>
            <Textarea
              id="testInstructions"
              rows={5}
              value={form.instructions ?? ""}
              onChange={(e) => setForm((f) => ({ ...f, instructions: e.target.value || null }))}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="testTimeLimit">Time limit (minutes)</Label>
              <Input
                id="testTimeLimit"
                type="number"
                min={5}
                max={480}
                value={form.timeLimitMinutes}
                onChange={(e) => setForm((f) => ({ ...f, timeLimitMinutes: Number(e.target.value) || 60 }))}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="testResponseWindow">Response window (days)</Label>
              <Input
                id="testResponseWindow"
                type="number"
                min={1}
                max={30}
                value={form.responseWindowDays}
                onChange={(e) => setForm((f) => ({ ...f, responseWindowDays: Number(e.target.value) || 5 }))}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="testPassThreshold">Pass threshold (0-100)</Label>
              <Input
                id="testPassThreshold"
                type="number"
                min={0}
                max={100}
                value={form.passThreshold}
                onChange={(e) => setForm((f) => ({ ...f, passThreshold: Number(e.target.value) || 0 }))}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="testRetakeCooldown">Retake cooldown (months)</Label>
              <Input
                id="testRetakeCooldown"
                type="number"
                min={0}
                max={24}
                value={form.retakeCooldownMonths}
                onChange={(e) => setForm((f) => ({ ...f, retakeCooldownMonths: Number(e.target.value) || 0 }))}
              />
            </div>
          </div>

          <DialogFooter>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Saving..." : "Save configuration"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
