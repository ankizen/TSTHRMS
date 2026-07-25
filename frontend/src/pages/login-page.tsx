import { zodResolver } from "@hookform/resolvers/zod"
import { ArrowRight, Building2, ShieldCheck, Sparkles, UsersRound } from "lucide-react"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuth } from "@/hooks/use-auth"

const loginSchema = z.object({
  email: z.string().min(1, "Email is required").email("Enter a valid email address"),
  password: z.string().min(1, "Password is required"),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage() {
  const { login, isLoggingIn, loginError } = useAuth()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) })

  const onSubmit = handleSubmit(async (values) => {
    await login(values).catch(() => undefined)
  })

  return (
    <div className="flex min-h-svh bg-background">
      {/* Marketing panel - hidden below md, since there's nothing to sign into it on a phone */}
      <div className="relative hidden w-1/2 flex-col justify-between overflow-hidden bg-gradient-to-br from-slate-950 via-blue-950 to-slate-900 p-12 text-white md:flex">
        <div
          className="pointer-events-none absolute inset-0 opacity-40"
          style={{
            backgroundImage:
              "radial-gradient(circle at 20% 20%, rgba(59,130,246,0.35), transparent 45%), radial-gradient(circle at 80% 70%, rgba(99,102,241,0.3), transparent 45%)",
          }}
        />

        <div className="relative flex items-center gap-2.5">
          <div className="flex size-9 items-center justify-center rounded-xl bg-white/10 backdrop-blur-sm ring-1 ring-white/15">
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
            <div className="flex items-center gap-3 rounded-2xl bg-white/[0.06] p-4 ring-1 ring-white/10 backdrop-blur-sm">
              <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-blue-500/20 text-blue-300">
                <UsersRound className="size-4.5" />
              </div>
              <div>
                <p className="text-sm font-medium text-white">Centralized employee records</p>
                <p className="text-xs text-slate-400">Personal, contact, and employment details in one place</p>
              </div>
            </div>
            <div className="flex items-center gap-3 rounded-2xl bg-white/[0.06] p-4 ring-1 ring-white/10 backdrop-blur-sm">
              <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-indigo-500/20 text-indigo-300">
                <ShieldCheck className="size-4.5" />
              </div>
              <div>
                <p className="text-sm font-medium text-white">Every change tracked</p>
                <p className="text-xs text-slate-400">Field-level audit history, automatically</p>
              </div>
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

      {/* Sign-in panel */}
      <div className="flex w-full flex-col items-center justify-center px-6 py-12 md:w-1/2">
        <div className="flex w-full max-w-sm flex-col gap-8">
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

          <form onSubmit={onSubmit} className="flex flex-col gap-5" noValidate>
            <div className="flex flex-col gap-2">
              <Label htmlFor="email">Email address</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                placeholder="you@company.com"
                className="h-11 rounded-xl"
                {...register("email")}
              />
              {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                className="h-11 rounded-xl"
                {...register("password")}
              />
              {errors.password && (
                <p className="text-sm text-destructive">{errors.password.message}</p>
              )}
            </div>
            {loginError && (
              <p className="text-sm text-destructive">
                Invalid email or password. Please try again.
              </p>
            )}
            <Button
              type="submit"
              disabled={isLoggingIn}
              className="h-11 rounded-full bg-gradient-to-r from-blue-600 to-blue-500 text-base shadow-lg shadow-blue-600/25 hover:from-blue-500 hover:to-blue-400"
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
