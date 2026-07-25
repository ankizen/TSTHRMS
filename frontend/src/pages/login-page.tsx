import { zodResolver } from "@hookform/resolvers/zod"
import axios from "axios"
import {
  ArrowRight,
  Building2,
  CheckCircle2,
  Eye,
  EyeOff,
  FileCheck2,
  ShieldCheck,
  Sparkles,
  UserPlus,
} from "lucide-react"
import { useState } from "react"
import { Controller, useForm } from "react-hook-form"
import { toast } from "sonner"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuth } from "@/hooks/use-auth"

const loginSchema = z.object({
  email: z.string().min(1, "Email is required").email("Enter a valid email address"),
  password: z.string().min(1, "Password is required"),
  rememberMe: z.boolean(),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage() {
  const { login, isLoggingIn, loginError } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { rememberMe: true },
  })

  const onSubmit = handleSubmit(async (values) => {
    await login(values).catch(() => undefined)
  })

  const isNetworkError = axios.isAxiosError(loginError) && !loginError.response

  return (
    <div className="flex min-h-svh bg-background">
      {/* Marketing panel - hidden below md, since there's nothing to sign into it on a phone */}
      <div className="relative hidden w-1/2 flex-col justify-between overflow-hidden bg-gradient-to-br from-slate-950 via-blue-950 to-slate-900 p-12 text-white md:flex">
        <div className="bg-grid pointer-events-none absolute inset-0 [mask-image:radial-gradient(ellipse_at_center,black_40%,transparent_75%)]" />
        <div
          className="animate-glow pointer-events-none absolute inset-0"
          style={{
            backgroundImage:
              "radial-gradient(circle at 20% 20%, rgba(59,130,246,0.35), transparent 45%), radial-gradient(circle at 80% 70%, rgba(99,102,241,0.3), transparent 45%)",
          }}
        />

        <div className="relative flex items-center gap-2.5">
          <div className="flex size-9 items-center justify-center rounded-xl bg-white/10 ring-1 ring-white/15 backdrop-blur-sm">
            <Building2 className="size-5" />
          </div>
          <span className="font-heading text-lg font-semibold tracking-tight">TSTHRMS</span>
        </div>

        <div className="relative flex flex-col gap-6">
          <span className="inline-flex w-fit items-center gap-1.5 rounded-full bg-white/10 px-3 py-1 text-xs font-medium text-blue-200 ring-1 ring-white/15">
            <Sparkles className="size-3.5" />
            Multi-tenant HR platform
          </span>
          <h1 className="font-heading text-4xl leading-[1.1] font-semibold tracking-tight text-balance">
            Every employee record,{" "}
            <span className="bg-gradient-to-r from-blue-300 to-indigo-200 bg-clip-text text-transparent">
              one workspace.
            </span>
          </h1>
          <p className="max-w-md text-sm leading-relaxed text-slate-300">
            Core HR, documents, and change history for every legal entity and product line you
            run - built to grow into the full employee lifecycle, one module at a time.
          </p>

          <div className="mt-2 flex flex-col gap-3">
            <div
              className="glass-card animate-float flex items-center gap-3 p-4"
              style={{ animationDelay: "0s" }}
            >
              <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-blue-500/20 text-blue-300">
                <UserPlus className="size-4.5" />
              </div>
              <div>
                <p className="text-sm font-medium text-white">Employee onboarded</p>
                <p className="text-xs text-slate-400">Master record created in Core HR</p>
              </div>
              <CheckCircle2 className="ml-auto size-4 shrink-0 text-emerald-400" />
            </div>
            <div
              className="glass-card animate-float flex items-center gap-3 p-4"
              style={{ animationDelay: "1.2s" }}
            >
              <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-indigo-500/20 text-indigo-300">
                <FileCheck2 className="size-4.5" />
              </div>
              <div>
                <p className="text-sm font-medium text-white">Document verified</p>
                <p className="text-xs text-slate-400">Education record marked verified</p>
              </div>
              <CheckCircle2 className="ml-auto size-4 shrink-0 text-emerald-400" />
            </div>
          </div>
        </div>

        <div className="relative flex items-center gap-4 text-xs text-slate-400">
          <span className="flex items-center gap-1.5">
            <ShieldCheck className="size-3.5" />
            Role-based access
          </span>
          <span className="h-3 w-px bg-white/15" />
          <span>Tenant-isolated by design</span>
        </div>
      </div>

      {/* Sign-in panel - extra horizontal padding keeps the form itself compact with breathing
          room around it, rather than stretching to fill the panel. */}
      <div className="flex w-full flex-col items-center justify-center px-6 py-12 md:w-1/2 md:px-16 lg:px-24">
        <div className="flex w-full max-w-[22rem] flex-col gap-8">
          <div className="flex items-center gap-2.5 md:hidden">
            <div className="flex size-9 items-center justify-center rounded-xl bg-primary text-primary-foreground">
              <Building2 className="size-5" />
            </div>
            <span className="font-heading text-lg font-semibold tracking-tight">TSTHRMS</span>
          </div>

          <div className="flex flex-col gap-1.5">
            <h1 className="font-heading text-3xl font-semibold tracking-tight">Welcome back</h1>
            <p className="text-sm text-muted-foreground">Sign in to your HR workspace.</p>
          </div>

          <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate>
            <div className="flex flex-col gap-2">
              <Label htmlFor="email">Email address</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                autoFocus
                placeholder="you@company.com"
                aria-invalid={Boolean(errors.email)}
                className="h-11 rounded-xl"
                {...register("email")}
              />
              {errors.email && (
                <p className="animate-in fade-in slide-in-from-top-1 text-sm text-destructive duration-200">
                  {errors.email.message}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <Label htmlFor="password">Password</Label>
                <button
                  type="button"
                  onClick={() => toast.info("Contact your HR Admin to reset your password.")}
                  className="text-xs font-medium text-primary transition-colors hover:text-primary/80 hover:underline"
                >
                  Forgot password?
                </button>
              </div>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  autoComplete="current-password"
                  aria-invalid={Boolean(errors.password)}
                  className="h-11 rounded-xl pr-10"
                  {...register("password")}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  aria-label={showPassword ? "Hide password" : "Show password"}
                  aria-pressed={showPassword}
                  className="absolute top-1/2 right-3 -translate-y-1/2 text-muted-foreground transition-colors hover:text-foreground"
                >
                  {showPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                </button>
              </div>
              {errors.password && (
                <p className="animate-in fade-in slide-in-from-top-1 text-sm text-destructive duration-200">
                  {errors.password.message}
                </p>
              )}
            </div>

            <div className="flex items-center gap-2">
              <Controller
                control={control}
                name="rememberMe"
                render={({ field }) => (
                  <Checkbox
                    id="rememberMe"
                    checked={field.value}
                    onCheckedChange={(checked) => field.onChange(checked === true)}
                  />
                )}
              />
              <Label htmlFor="rememberMe" className="cursor-pointer font-normal text-muted-foreground">
                Keep me signed in on this device
              </Label>
            </div>

            {loginError && (
              <p className="animate-in fade-in slide-in-from-top-1 rounded-lg bg-destructive/10 px-3 py-2 text-sm text-destructive duration-200">
                {isNetworkError
                  ? "Network error - check your connection and try again."
                  : "Invalid email or password. Please try again."}
              </p>
            )}

            <Button
              type="submit"
              isLoading={isLoggingIn}
              className="mt-1 h-11 rounded-full bg-gradient-to-r from-blue-600 to-blue-500 text-base shadow-lg shadow-blue-600/25 hover:from-blue-500 hover:to-blue-400"
            >
              {isLoggingIn ? "Signing in..." : "Sign in"}
              {!isLoggingIn && <ArrowRight />}
            </Button>
          </form>

          <p className="text-center text-xs text-muted-foreground">
            This is a private company workspace - your HR Admin sends invites for new logins.
          </p>
        </div>
      </div>
    </div>
  )
}
