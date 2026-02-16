import { useState } from 'react';
import { useCompanyLogo } from '@/hooks/useCompanyLogo';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { InputOTP, InputOTPGroup, InputOTPSlot } from '@/components/ui/input-otp';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { ArrowLeft, Eye, EyeOff } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { toast } from '@/hooks/use-toast';

interface ForgotPasswordFormProps {
  onBack: () => void;
}

type Step = 'email' | 'otp' | 'reset';

export function ForgotPasswordForm({ onBack }: ForgotPasswordFormProps) {
  const { t } = useTranslation();
  const companyLogo = useCompanyLogo();
  const [step, setStep] = useState<Step>('email');
  const [email, setEmail] = useState('');
  const [otp, setOtp] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleEmailSubmit = async () => {
    if (!email) {
      toast({
        title: t('auth.error'),
        description: t('auth.forgot_password.email_required'),
        variant: 'destructive',
      });
      return;
    }

    setIsLoading(true);
    // Simulate sending OTP
    await new Promise(resolve => setTimeout(resolve, 1500));
    
    toast({
      title: t('auth.success'),
      description: t('auth.forgot_password.otp_sent'),
    });
    
    setStep('otp');
    setIsLoading(false);
  };

  const handleOtpSubmit = async () => {
    if (otp.length !== 6) {
      toast({
        title: t('auth.error'),
        description: t('auth.forgot_password.otp_required'),
        variant: 'destructive',
      });
      return;
    }

    setIsLoading(true);
    // Simulate OTP verification
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    toast({
      title: t('auth.success'),
      description: t('auth.forgot_password.otp_verified'),
    });
    
    setStep('reset');
    setIsLoading(false);
  };

  const handlePasswordReset = async () => {
    if (!newPassword || !confirmPassword) {
      toast({
        title: t('auth.error'),
        description: t('auth.forgot_password.password_required'),
        variant: 'destructive',
      });
      return;
    }

    if (newPassword !== confirmPassword) {
      toast({
        title: t('auth.error'),
        description: t('auth.forgot_password.passwords_dont_match'),
        variant: 'destructive',
      });
      return;
    }

    if (newPassword.length < 6) {
      toast({
        title: t('auth.error'),
        description: t('auth.forgot_password.password_too_short'),
        variant: 'destructive',
      });
      return;
    }

    setIsLoading(true);
    // Simulate password reset
    await new Promise(resolve => setTimeout(resolve, 1500));
    
    toast({
      title: t('auth.success'),
      description: t('auth.forgot_password.password_reset_success'),
    });
    
    // Go back to login
    onBack();
    setIsLoading(false);
  };

  return (
    <div className="w-full max-w-lg mx-auto">
      {/* Logo */}
      <div className="text-center mb-6">
        {companyLogo && (
          <div className="mb-4">
            <img 
              src={companyLogo} 
              alt="Company Logo" 
              className="h-18 mx-auto"
            />
          </div>
        )}
      </div>

      <Card className="border-0 shadow-2xl bg-card/95 backdrop-blur-xl">
        <CardHeader className="space-y-4 pb-4 px-6 sm:px-8 pt-6">
          <div className="flex items-center space-x-4">
            <Button
              variant="ghost"
              size="sm"
              onClick={onBack}
              className="p-1 h-8 w-8"
            >
              <ArrowLeft className="h-4 w-4" />
            </Button>
            <CardTitle className="text-xl font-semibold">
              {t('auth.forgot_password.title')}
            </CardTitle>
          </div>

          {step === 'email' && (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                {t('auth.forgot_password.email_description')}
              </p>
              <div className="space-y-1.5">
                <label htmlFor="reset-email" className="text-sm font-semibold text-foreground/90">
                  {t('auth.email')}
                </label>
                <Input
                  id="reset-email"
                  type="email"
                  placeholder={t('auth.email_placeholder')}
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="h-10 bg-background/80 border-border/60 focus:border-primary/60 focus:ring-2 focus:ring-primary/20 rounded-lg text-sm transition-all duration-200"
                />
              </div>
            </div>
          )}

          {step === 'otp' && (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                {t('auth.forgot_password.otp_description', { email })}
              </p>
              <div className="space-y-1.5">
                <label className="text-sm font-semibold text-foreground/90">
                  {t('auth.forgot_password.verification_code')}
                </label>
                <div className="flex justify-center">
                  <InputOTP 
                    value={otp} 
                    onChange={(value) => setOtp(value)}
                    maxLength={6}
                  >
                    <InputOTPGroup>
                      <InputOTPSlot index={0} />
                      <InputOTPSlot index={1} />
                      <InputOTPSlot index={2} />
                      <InputOTPSlot index={3} />
                      <InputOTPSlot index={4} />
                      <InputOTPSlot index={5} />
                    </InputOTPGroup>
                  </InputOTP>
                </div>
              </div>
            </div>
          )}

          {step === 'reset' && (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                {t('auth.forgot_password.reset_description')}
              </p>
              <div className="space-y-1.5">
                <label htmlFor="new-password" className="text-sm font-semibold text-foreground/90">
                  {t('auth.forgot_password.new_password')}
                </label>
                <div className="relative">
                  <Input
                    id="new-password"
                    type={showNewPassword ? "text" : "password"}
                    placeholder={t('auth.forgot_password.new_password_placeholder')}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    autoComplete="new-password"
                    className="h-10 bg-background/80 border-border/60 focus:border-primary/60 focus:ring-2 focus:ring-primary/20 rounded-lg text-sm pr-10 transition-all duration-200 [&::-ms-reveal]:hidden [&::-ms-clear]:hidden [&::-webkit-contacts-auto-fill-button]:hidden [&::-webkit-credentials-auto-fill-button]:hidden"
                  />
                  <button
                    type="button"
                    onClick={() => setShowNewPassword(!showNewPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors p-0.5 rounded-md hover:bg-muted/50"
                  >
                    {showNewPassword ? (
                      <EyeOff className="h-3.5 w-3.5" />
                    ) : (
                      <Eye className="h-3.5 w-3.5" />
                    )}
                  </button>
                </div>
              </div>
              
              <div className="space-y-1.5">
                <label htmlFor="confirm-password" className="text-sm font-semibold text-foreground/90">
                  {t('auth.forgot_password.confirm_password')}
                </label>
                <div className="relative">
                  <Input
                    id="confirm-password"
                    type={showConfirmPassword ? "text" : "password"}
                    placeholder={t('auth.forgot_password.confirm_password_placeholder')}
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    autoComplete="new-password"
                    className="h-10 bg-background/80 border-border/60 focus:border-primary/60 focus:ring-2 focus:ring-primary/20 rounded-lg text-sm pr-10 transition-all duration-200 [&::-ms-reveal]:hidden [&::-ms-clear]:hidden [&::-webkit-contacts-auto-fill-button]:hidden [&::-webkit-credentials-auto-fill-button]:hidden"
                  />
                  <button
                    type="button"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors p-0.5 rounded-md hover:bg-muted/50"
                  >
                    {showConfirmPassword ? (
                      <EyeOff className="h-3.5 w-3.5" />
                    ) : (
                      <Eye className="h-3.5 w-3.5" />
                    )}
                  </button>
                </div>
              </div>
            </div>
          )}
        </CardHeader>
        
        <CardContent className="space-y-4 px-6 sm:px-8 pb-6">
          <Button
            onClick={() => {
              if (step === 'email') handleEmailSubmit();
              else if (step === 'otp') handleOtpSubmit();
              else handlePasswordReset();
            }}
            className="w-full h-10 bg-gradient-to-r from-primary to-primary/90 hover:from-primary/90 hover:to-primary text-primary-foreground font-semibold text-sm rounded-lg shadow-lg hover:shadow-xl transition-all duration-200 transform hover:scale-[1.02]"
            disabled={isLoading}
          >
            {isLoading ? (
              <div className="flex items-center justify-center space-x-2">
                <div className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin" />
                <span>
                  {step === 'email' && t('auth.forgot_password.sending')}
                  {step === 'otp' && t('auth.forgot_password.verifying')}
                  {step === 'reset' && t('auth.forgot_password.resetting')}
                </span>
              </div>
            ) : (
              <span>
                {step === 'email' && t('auth.forgot_password.send_code')}
                {step === 'otp' && t('auth.forgot_password.verify_code')}
                {step === 'reset' && t('auth.forgot_password.reset_password')}
              </span>
            )}
          </Button>

          {step === 'otp' && (
            <div className="text-center">
              <p className="text-xs text-muted-foreground">
                {t('auth.forgot_password.didnt_receive_code')}
                <button
                  onClick={handleEmailSubmit}
                  className="ml-1.5 text-primary hover:text-primary/80 font-semibold transition-colors"
                  disabled={isLoading}
                >
                  {t('auth.forgot_password.resend')}
                </button>
              </p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}