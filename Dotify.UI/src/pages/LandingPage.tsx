import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'

export function LandingPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-4">
      <Card className="w-full max-w-md text-center">
        <CardHeader>
          <CardTitle className="text-4xl font-bold tracking-tight">
            Dotify
          </CardTitle>
          <CardDescription className="text-base">
            Adam's self hosted music streaming service.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button disabled className="w-full">
            Log in
          </Button>
        </CardContent>
      </Card>
    </main>
  )
}
