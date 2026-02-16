
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { User, Shield, Settings, Eye, EyeOff } from "lucide-react";
import { ProfilePictureUpload } from "@/components/ui/profile-picture-upload";
import { useAuth } from "@/contexts/AuthContext";
import { useState, useEffect } from "react";
import { useTranslation } from 'react-i18next';
import { toast } from "@/hooks/use-toast";
import { authService } from "@/services/authService";
import { preferencesService, UserPreferences } from "@/services/preferencesService";
import { CompanySettings } from "./CompanySettings";

interface AccountSettingsProps {
  section?: 'profile' | 'company' | 'security';
}

export function AccountSettings({ section }: AccountSettingsProps = {}) {
  const { user, updateUser, refreshUser } = useAuth();
  const { t } = useTranslation('settings');
  const [isUpdatingProfile, setIsUpdatingProfile] = useState(false);
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [isUpdatingPreferences, setIsUpdatingPreferences] = useState(false);
  const [profileData, setProfileData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    companyName: ''
  });
  const [passwordData, setPasswordData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [preferences, setPreferences] = useState<UserPreferences>({
    theme: 'system',
    language: 'en',
    primaryColor: 'blue',
    layoutMode: 'sidebar',
    dataView: 'table',
    timezone: 'UTC',
    dateFormat: 'MM/DD/YYYY',
    timeFormat: '12h',
    currency: 'USD',
    sidebarCollapsed: false,
    compactMode: false,
    showTooltips: true,
    animationsEnabled: true,
    soundEnabled: false,
    autoSave: true,
  });

  useEffect(() => {
    if (user) {
      setProfileData({
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        email: user.email || '',
        phoneNumber: user.phoneNumber || '',
        companyName: user.companyName || ''
      });
      
      // Fetch user preferences
      loadPreferences();
    }
  }, [user]);

  const loadPreferences = async () => {
    try {
      const userPrefs = await preferencesService.getUserPreferences();
      if (userPrefs) {
        setPreferences(userPrefs);
      } else {
        // Use local preferences as fallback
        const localPrefs = preferencesService.getLocalPreferences();
        setPreferences(localPrefs);
      }
    } catch (error) {
      console.error('Error loading preferences:', error);
      const localPrefs = preferencesService.getLocalPreferences();
      setPreferences(localPrefs);
    }
  };

  const handleProfileUpdate = async () => {
    setIsUpdatingProfile(true);
    try {
      const success = await updateUser({
        firstName: profileData.firstName,
        lastName: profileData.lastName,
        phoneNumber: profileData.phoneNumber,
        companyName: profileData.companyName
      });
      
      if (success) {
        toast({
          title: t('account.profileUpdatedTitle'),
          description: t('account.profileUpdatedDesc'),
        });
      } else {
        toast({
          title: t('account.preferencesUpdateFailed') || t('account.profileUpdateFailed') || 'Update failed',
          description: t('account.profileUpdateFailed') || 'Failed to update profile. Please try again.',
          variant: "destructive"
        });
      }
    } catch (error) {
      console.error('Profile update error:', error);
      toast({
        title: t('account.profileUpdateFailed') || 'Update failed',
        description: t('account.profileUpdateFailed') || 'An error occurred while updating your profile.',
        variant: "destructive"
      });
    } finally {
      setIsUpdatingProfile(false);
    }
  };

  const handlePasswordChange = async () => {
    if (passwordData.newPassword !== passwordData.confirmPassword) {
      toast({
        title: "Password mismatch",
        description: "New password and confirmation do not match.",
        variant: "destructive"
      });
      return;
    }

    setIsChangingPassword(true);
    try {
      const response = await authService.changePassword(passwordData);
      
      if (response.success) {
        toast({
          title: t('account.passwordChangedTitle'),
          description: t('account.passwordChangedDesc'),
        });
        setPasswordData({
          currentPassword: '',
          newPassword: '',
          confirmPassword: ''
        });
      } else {
        toast({
          title: t('account.passwordChangeFailedTitle') || 'Password change failed',
          description: response.message || t('account.passwordChangeFailedDesc') || 'Failed to change password. Please try again.',
          variant: "destructive"
        });
      }
    } catch (error) {
      console.error('Password change error:', error);
      toast({
        title: t('account.passwordChangeFailedTitle') || 'Password change failed',
        description: t('account.passwordChangeFailedDesc') || 'An error occurred while changing your password.',
        variant: "destructive"
      });
    } finally {
      setIsChangingPassword(false);
    }
  };

  const handlePreferencesUpdate = async () => {
    setIsUpdatingPreferences(true);
    try {
      const result = await preferencesService.updateUserPreferences(preferences);
      
      if (result) {
        preferencesService.savePreferencesLocally(preferences);
        toast({
          title: t('account.preferencesUpdatedTitle'),
          description: t('account.preferencesUpdatedDesc'),
        });
        
        // Apply theme change if needed
        if (preferences.theme !== 'system') {
          document.documentElement.classList.remove('light', 'dark');
          document.documentElement.classList.add(preferences.theme);
        } else {
          const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
          document.documentElement.classList.remove('light', 'dark');
          document.documentElement.classList.add(isDark ? 'dark' : 'light');
        }
      } else {
        toast({
          title: t('account.preferencesUpdateFailed') || 'Update failed',
          description: t('account.preferencesUpdateFailed') || 'Failed to update preferences. Please try again.',
          variant: "destructive"
        });
      }
    } catch (error) {
      console.error('Preferences update error:', error);
      toast({
        title: t('account.preferencesUpdateFailed') || 'Update failed',
        description: t('account.preferencesUpdateFailed') || 'An error occurred while updating your preferences.',
        variant: "destructive"
      });
    } finally {
      setIsUpdatingPreferences(false);
    }
  };

  const showProfile = !section || section === 'profile';
  const showCompany = !section || section === 'company';
  const showSecurity = !section || section === 'security';

  return (
    <div className="space-y-4 sm:space-y-6">
      {/* Profile Settings */}
      {showProfile && (
      <Card className="shadow-card border-0 bg-card">
        <CardHeader className="p-4 sm:p-6">
            <CardTitle className="text-sm font-medium text-foreground flex items-center gap-2">
              <User className="h-4 w-4 text-muted-foreground" />
              {t('account.profileTitle')}
            </CardTitle>
            <CardDescription className="text-xs">{t('account.profileDesc')}</CardDescription>
          </CardHeader>
        <CardContent className="p-4 sm:p-6 space-y-4">
          {/* Profile Picture */}
          <ProfilePictureUpload
            currentUrl={(user as any)?.profilePictureUrl}
            onUploaded={async (url) => {
              try {
                console.log('[AccountSettings] Saving profilePictureUrl via dedicated API:', url);
                const { profilePictureApi } = await import('@/services/api/profilePictureApi');
                const result = await profilePictureApi.updateAdminProfilePicture(url);
                if (result.success) {
                  // Update localStorage with new user data
                  if (result.user) {
                    const userData = localStorage.getItem('user_data');
                    if (userData) {
                      const parsed = JSON.parse(userData);
                      parsed.profilePictureUrl = url;
                      localStorage.setItem('user_data', JSON.stringify(parsed));
                    }
                  }
                  // Refresh user state in context
                  await refreshUser();
                  toast({ title: 'Profile picture saved', description: 'Your profile picture has been updated successfully.' });
                } else {
                  toast({ title: 'Failed to save profile picture', description: result.message || 'Could not save profile picture URL.', variant: 'destructive' });
                }
              } catch (e: any) {
                console.error('Failed to save profile picture:', e);
                toast({ title: 'Failed to save profile picture', description: e.message || 'An error occurred.', variant: 'destructive' });
              }
            }}
            onRemoved={async () => {
              try {
                const { profilePictureApi } = await import('@/services/api/profilePictureApi');
                await profilePictureApi.removeAdminProfilePicture();
                const userData = localStorage.getItem('user_data');
                if (userData) {
                  const parsed = JSON.parse(userData);
                  parsed.profilePictureUrl = null;
                  localStorage.setItem('user_data', JSON.stringify(parsed));
                }
                await refreshUser();
                toast({ title: 'Profile picture removed' });
              } catch (e: any) {
                console.error('Failed to remove profile picture:', e);
                toast({ title: 'Failed to remove profile picture', description: e.message || 'An error occurred.', variant: 'destructive' });
              }
            }}
          />
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="firstName" className="text-sm font-medium">{t('account.firstName')}</Label>
              <Input 
                id="firstName" 
                value={profileData.firstName}
                onChange={(e) => setProfileData(prev => ({ ...prev, firstName: e.target.value }))}
                className="h-9 sm:h-10" 
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="lastName" className="text-sm font-medium">{t('account.lastName')}</Label>
              <Input 
                id="lastName" 
                value={profileData.lastName}
                onChange={(e) => setProfileData(prev => ({ ...prev, lastName: e.target.value }))}
                className="h-9 sm:h-10" 
              />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="email">{t('account.email')}</Label>
            <Input 
              id="email" 
              type="email" 
              value={profileData.email}
              disabled
              className="bg-muted"
            />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="phone">{t('account.phone')}</Label>
              <Input 
                id="phone" 
                type="tel" 
                value={profileData.phoneNumber}
                onChange={(e) => setProfileData(prev => ({ ...prev, phoneNumber: e.target.value }))}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="company">{t('account.company')}</Label>
              <Input 
                id="company" 
                value={profileData.companyName}
                onChange={(e) => setProfileData(prev => ({ ...prev, companyName: e.target.value }))}
              />
            </div>
          </div>
          <div className="flex justify-end pt-2">
            <Button 
              onClick={handleProfileUpdate}
              disabled={isUpdatingProfile}
              className="gradient-primary text-white shadow-medium hover-lift"
            >
              {isUpdatingProfile ? t('account.updating') : t('account.updateProfile')}
            </Button>
          </div>
        </CardContent>
      </Card>
      )}

      {/* Company Information */}
      {showCompany && <CompanySettings />}

      {/* Security Settings */}
      {showSecurity && (
      <Card className="shadow-card border-0 bg-card">
        <CardHeader className="p-4 sm:p-6">
          <CardTitle className="text-sm font-medium text-foreground flex items-center gap-2">
            <Shield className="h-4 w-4 text-muted-foreground" />
            {t('account.securityTitle')}
          </CardTitle>
          <CardDescription className="text-xs">{t('account.securityDesc')}</CardDescription>
        </CardHeader>
        <CardContent className="p-4 sm:p-6 space-y-4">
          <div className="space-y-2">
            <Label htmlFor="currentPassword" className="text-sm font-medium">{t('account.currentPassword')}</Label>
            <div className="relative">
              <Input 
                id="currentPassword" 
                type={showCurrentPassword ? "text" : "password"} 
                value={passwordData.currentPassword}
                onChange={(e) => setPasswordData(prev => ({ ...prev, currentPassword: e.target.value }))}
                className="h-9 sm:h-10 pr-10" 
              />
              <button
                type="button"
                onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
              >
                {showCurrentPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="newPassword" className="text-sm font-medium">{t('account.newPassword')}</Label>
              <div className="relative">
                <Input 
                  id="newPassword" 
                  type={showNewPassword ? "text" : "password"} 
                  value={passwordData.newPassword}
                  onChange={(e) => setPasswordData(prev => ({ ...prev, newPassword: e.target.value }))}
                  className="h-9 sm:h-10 pr-10" 
                />
                <button
                  type="button"
                  onClick={() => setShowNewPassword(!showNewPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                >
                  {showNewPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="confirmPassword" className="text-sm font-medium">{t('account.confirmPassword')}</Label>
              <div className="relative">
                <Input 
                  id="confirmPassword" 
                  type={showConfirmPassword ? "text" : "password"} 
                  value={passwordData.confirmPassword}
                  onChange={(e) => setPasswordData(prev => ({ ...prev, confirmPassword: e.target.value }))}
                  className="h-9 sm:h-10 pr-10" 
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                >
                  {showConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
            </div>
          </div>
          <div className="flex justify-end pt-2">
            <Button 
              onClick={handlePasswordChange}
              disabled={isChangingPassword || !passwordData.currentPassword || !passwordData.newPassword || !passwordData.confirmPassword}
              className="gradient-primary text-white shadow-medium hover-lift"
            >
              {isChangingPassword ? t('account.changing') : t('account.changePassword')}
            </Button>
          </div>
        </CardContent>
      </Card>
      )}
    </div>
  );
}
