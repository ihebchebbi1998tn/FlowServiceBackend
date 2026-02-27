import { useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Button } from "@/components/ui/button";
import { useTheme } from "@/hooks/useTheme";
import { usePreferences } from "@/hooks/usePreferences";
import { usePermissions } from "@/hooks/usePermissions";
import { Palette, Sun, Moon, Monitor, Sidebar, Layout, Table, List, Loader2, Save, Check } from "lucide-react";
import { useTranslation } from "react-i18next";
import i18n from "@/lib/i18n";
import { toast } from "@/hooks/use-toast";
import { UnsavedChangesDialog } from "@/components/dialogs/UnsavedChangesDialog";

const languages = [
  { code: 'en', name: 'English' },
  { code: 'fr', name: 'Français' }
];

const primaryColors = [
  { value: 'blue', name: 'Blue', color: 'bg-blue-500' },
  { value: 'red', name: 'Red', color: 'bg-red-500' },
  { value: 'green', name: 'Green', color: 'bg-green-500' },
  { value: 'purple', name: 'Purple', color: 'bg-purple-500' },
  { value: 'orange', name: 'Orange', color: 'bg-orange-500' },
  { value: 'indigo', name: 'Indigo', color: 'bg-indigo-500' }
];

const layoutModes = [
  { value: 'sidebar', labelKey: 'preferences.layout.sidebar', descKey: 'preferences.layout.sidebarDesc', icon: Sidebar },
  { value: 'topbar', labelKey: 'preferences.layout.topbar', descKey: 'preferences.layout.topbarDesc', icon: Layout }
] as const;

const dataViews = [
  { value: 'table', labelKey: 'preferences.dataView.table', descKey: 'preferences.dataView.tableDesc', icon: Table },
  { value: 'list', labelKey: 'preferences.dataView.list', descKey: 'preferences.dataView.listDesc', icon: List }
] as const;

const themes = [
  { value: 'light', labelKey: 'preferences.theme.light', icon: Sun },
  { value: 'dark', labelKey: 'preferences.theme.dark', icon: Moon },
  { value: 'system', labelKey: 'preferences.theme.system', icon: Monitor }
] as const;

export function UserPreferencesTab() {
  const { t } = useTranslation('settings');
  const navigate = useNavigate();
  const location = useLocation();
  const { preferences, updatePreferences, applyColorTheme } = usePreferences();
  const { setTheme } = useTheme();
  const { hasPermission, isMainAdmin } = usePermissions();
  
  // Check if user can edit settings based on permissions
  const canEditSettings = isMainAdmin || hasPermission('settings', 'update');
  
  const [isSaving, setIsSaving] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);
  const [showUnsavedDialog, setShowUnsavedDialog] = useState(false);
  const [pendingNavigation, setPendingNavigation] = useState<string | null>(null);
  
  const [formData, setFormData] = useState({
    theme: 'system' as 'light' | 'dark' | 'system',
    language: 'en',
    primaryColor: 'blue',
    layoutMode: 'sidebar' as 'sidebar' | 'topbar',
    dataView: 'table' as 'table' | 'list'
  });

  // Store original data to compare changes
  const [originalData, setOriginalData] = useState(formData);

  // Initialize form with current preferences ONLY on first load
  const [initialized, setInitialized] = useState(false);
  
  useEffect(() => {
    if (!initialized) {
      // Priority 1: Try to get from user_data.preferences (MainAdminUsers.PreferencesJson)
      const userData = localStorage.getItem('user_data');
      if (userData) {
        try {
          const user = JSON.parse(userData);
          if (user.preferences) {
            const prefs = typeof user.preferences === 'string'
              ? JSON.parse(user.preferences)
              : user.preferences;
            const initialData = {
              theme: prefs.theme || 'system',
              language: prefs.language || 'en',
              primaryColor: prefs.primaryColor || 'blue',
              layoutMode: prefs.layoutMode || 'sidebar',
              dataView: prefs.dataView || 'table'
            };
            setFormData(initialData);
            setOriginalData(initialData);
            setInitialized(true);
            return;
          }
        } catch (error) {
          console.error('Error parsing user preferences from user_data:', error);
        }
      }
      
      // Priority 2: Try preferences context
      if (preferences && Object.keys(preferences).length > 0) {
        const initialData = {
          theme: (preferences.theme as 'light' | 'dark' | 'system') || 'system',
          language: preferences.language || 'en',
          primaryColor: preferences.primaryColor || 'blue',
          layoutMode: (preferences.layoutMode as 'sidebar' | 'topbar') || 'sidebar',
          dataView: (preferences.dataView as 'table' | 'list') || 'table'
        };
        setFormData(initialData);
        setOriginalData(initialData);
        setInitialized(true);
        return;
      }
      
      // Priority 3: Try localStorage user-preferences
      const localPrefs = localStorage.getItem('user-preferences');
      if (localPrefs) {
        try {
          const prefs = JSON.parse(localPrefs);
          const initialData = {
            theme: prefs.theme || 'system',
            language: prefs.language || 'en',
            primaryColor: prefs.primaryColor || 'blue',
            layoutMode: prefs.layoutMode || 'sidebar',
            dataView: prefs.dataView || 'table'
          };
          setFormData(initialData);
          setOriginalData(initialData);
          setInitialized(true);
        } catch (error) {
          console.error('Error parsing local preferences:', error);
        }
      }
    }
  }, [preferences, initialized]);

  // Detect actual changes by comparing with original data
  useEffect(() => {
    const changed = JSON.stringify(formData) !== JSON.stringify(originalData);
    setHasChanges(changed);
  }, [formData, originalData]);

  // Browser beforeunload event for tab close/refresh
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (hasChanges && canEditSettings) {
        e.preventDefault();
        e.returnValue = '';
        return '';
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [hasChanges, canEditSettings]);

  // Handle navigation blocking via click interception
  useEffect(() => {
    if (!hasChanges || !canEditSettings) return;

    const handleClick = (e: MouseEvent) => {
      const target = e.target as HTMLElement;
      const link = target.closest('a[href]');
      
      if (link) {
        const href = link.getAttribute('href');
        // Only intercept internal navigation links
        if (href && href.startsWith('/') && !href.includes(location.pathname)) {
          e.preventDefault();
          e.stopPropagation();
          setPendingNavigation(href);
          setShowUnsavedDialog(true);
        }
      }
    };

    // Intercept sidebar and navigation clicks
    document.addEventListener('click', handleClick, true);
    return () => document.removeEventListener('click', handleClick, true);
  }, [hasChanges, canEditSettings, location.pathname]);

  // Only apply theme/color/language changes AFTER initialization to prevent flash
  useEffect(() => {
    if (initialized) {
      setTheme(formData.theme);
    }
  }, [formData.theme, setTheme, initialized]);

  useEffect(() => {
    if (initialized) {
      applyColorTheme(formData.primaryColor);
    }
  }, [formData.primaryColor, applyColorTheme, initialized]);

  // Apply language change only after initialization
  useEffect(() => {
    if (initialized && formData.language && i18n.language !== formData.language) {
      i18n.changeLanguage(formData.language);
      // Persist language preference to localStorage (both keys for compatibility)
      localStorage.setItem('language', formData.language);
    }
  }, [formData.language, initialized]);

  const handleChange = (key: string, value: string) => {
    setFormData(prev => ({ ...prev, [key]: value }));
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      console.log('[UserPreferencesTab] Saving preferences:', formData);
      
      // Use updatePreferences which handles all saving (local + backend + user_data sync)
      await updatePreferences(formData);
      
      toast({ 
        title: t('preferences.preferencesSaved'), 
        description: t('preferences.preferencesSavedDesc') 
      });
      
      // Update original data to match saved data
      setOriginalData(formData);
      setHasChanges(false);
    } catch (error: any) {
      console.error('[UserPreferencesTab] Save error:', error);
      toast({ title: t('account.preferencesUpdateFailed'), description: error.message, variant: "destructive" });
    } finally {
      setIsSaving(false);
    }
  };

  // Dialog handlers
  const handleDialogSave = async () => {
    await handleSave();
    setShowUnsavedDialog(false);
    if (pendingNavigation) {
      navigate(pendingNavigation);
      setPendingNavigation(null);
    }
  };

  const handleDialogDiscard = () => {
    // Reset form to original data
    setFormData(originalData);
    setHasChanges(false);
    setShowUnsavedDialog(false);
    if (pendingNavigation) {
      navigate(pendingNavigation);
      setPendingNavigation(null);
    }
  };

  const handleDialogCancel = () => {
    setShowUnsavedDialog(false);
    setPendingNavigation(null);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      {/* Appearance Card */}
      <Card className="shadow-card border-0 bg-card">
        <CardHeader className="p-4 sm:p-6">
            <CardTitle className="text-sm font-medium text-foreground flex items-center gap-2">
            <Palette className="h-4 w-4 text-primary" />
            {t('preferences.appearanceTitle')}
          </CardTitle>
          <CardDescription className="text-xs">
            {t('preferences.appearanceDesc')}
          </CardDescription>
        </CardHeader>
        <CardContent className={`p-4 sm:p-6 space-y-6 ${!canEditSettings ? 'opacity-75 pointer-events-none' : ''}`}>
          {/* Theme */}
          <div className="space-y-3">
            <Label className="text-sm font-medium">{t('preferences.themeLabel')}</Label>
            <RadioGroup 
              value={formData.theme} 
              onValueChange={(value: 'light' | 'dark' | 'system') => handleChange('theme', value)} 
              className="grid grid-cols-3 gap-3"
              disabled={!canEditSettings}
            >
              {themes.map(theme => {
                const IconComponent = theme.icon;
                const isSelected = formData.theme === theme.value;
                return (
                  <div key={theme.value} className="relative">
                    <RadioGroupItem value={theme.value} id={`theme-${theme.value}`} className="peer sr-only" disabled={!canEditSettings} />
                    <Label 
                      htmlFor={`theme-${theme.value}`} 
                      className={`flex flex-col items-center justify-center p-3 border-2 rounded-xl transition-all ${isSelected ? 'border-primary bg-primary/10' : ''} ${!canEditSettings ? 'cursor-not-allowed' : 'cursor-pointer hover:border-primary/50 hover:bg-primary/5'}`}
                    >
                      <IconComponent className={`h-5 w-5 mb-1 ${isSelected ? 'text-primary' : ''}`} />
                      <span className={`text-sm font-medium ${isSelected ? 'text-primary' : ''}`}>{t(theme.labelKey)}</span>
                    </Label>
                    {isSelected && (
                      <div className="absolute -top-1 -right-1 w-5 h-5 bg-primary rounded-full flex items-center justify-center">
                        <Check className="h-3 w-3 text-primary-foreground" />
                      </div>
                    )}
                  </div>
                );
              })}
            </RadioGroup>
          </div>

          {/* Primary Color */}
          <div className="space-y-3">
            <Label className="text-sm font-medium">{t('preferences.primaryColorLabel')}</Label>
            <RadioGroup 
              value={formData.primaryColor} 
              onValueChange={(value: string) => handleChange('primaryColor', value)} 
              className="flex flex-wrap gap-3"
              disabled={!canEditSettings}
            >
              {primaryColors.map(color => (
                <div key={color.value}>
                  <RadioGroupItem value={color.value} id={`color-${color.value}`} className="peer sr-only" disabled={!canEditSettings} />
                  <Label 
                    htmlFor={`color-${color.value}`} 
                    className={`block ${!canEditSettings ? 'cursor-not-allowed' : 'cursor-pointer'}`}
                  >
                    <div className={`w-10 h-10 rounded-full ${color.color} ring-2 ring-offset-2 ring-offset-background transition-all ${formData.primaryColor === color.value ? 'ring-primary scale-110' : 'ring-transparent'} ${canEditSettings ? 'hover:ring-muted-foreground/30' : ''}`} />
                  </Label>
                </div>
              ))}
            </RadioGroup>
          </div>

          {/* Language */}
          <div className="flex items-center justify-between">
            <div>
              <Label className="text-sm font-medium">{t('preferences.languageLabel')}</Label>
              <p className="text-sm text-muted-foreground">
                {t('preferences.languageDesc')}
              </p>
            </div>
            <Select value={formData.language} onValueChange={value => handleChange('language', value)} disabled={!canEditSettings}>
              <SelectTrigger className={`w-[160px] bg-background ${!canEditSettings ? 'cursor-not-allowed' : ''}`}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent className="bg-popover border shadow-lg z-50">
                {languages.map(lang => (
                  <SelectItem key={lang.code} value={lang.code}>{lang.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Layout Card */}
      <Card className="shadow-card border-0 bg-card">
        <CardHeader className="p-4 sm:p-6">
          <CardTitle className="text-sm font-medium text-foreground flex items-center gap-2">
            <Layout className="h-4 w-4 text-primary" />
            {t('preferences.layoutTitle')}
          </CardTitle>
          <CardDescription className="text-xs">
            {t('preferences.layoutDesc')}
          </CardDescription>
        </CardHeader>
        <CardContent className={`p-4 sm:p-6 space-y-6 ${!canEditSettings ? 'opacity-75 pointer-events-none' : ''}`}>
          {/* Layout Mode */}
          <div className="space-y-3">
            <Label className="text-sm font-medium">{t('preferences.navigationStyleLabel')}</Label>
            <RadioGroup 
              value={formData.layoutMode} 
              onValueChange={(value: 'sidebar' | 'topbar') => handleChange('layoutMode', value)} 
              className="grid grid-cols-2 gap-3"
              disabled={!canEditSettings}
            >
              {layoutModes.map(layout => {
                const IconComponent = layout.icon;
                const isSelected = formData.layoutMode === layout.value;
                return (
                  <div key={layout.value} className="relative">
                    <RadioGroupItem value={layout.value} id={`layout-${layout.value}`} className="peer sr-only" disabled={!canEditSettings} />
                    <Label 
                      htmlFor={`layout-${layout.value}`} 
                      className={`flex items-center gap-3 p-4 border-2 rounded-xl transition-all ${isSelected ? 'border-primary bg-primary/10' : ''} ${!canEditSettings ? 'cursor-not-allowed' : 'cursor-pointer hover:border-primary/50 hover:bg-primary/5'}`}
                    >
                      <IconComponent className={`h-5 w-5 ${isSelected ? 'text-primary' : ''}`} />
                      <div>
                        <span className={`font-medium block ${isSelected ? 'text-primary' : ''}`}>{t(layout.labelKey)}</span>
                        <span className="text-xs text-muted-foreground">{t(layout.descKey)}</span>
                      </div>
                    </Label>
                    {isSelected && (
                      <div className="absolute -top-1 -right-1 w-5 h-5 bg-primary rounded-full flex items-center justify-center">
                        <Check className="h-3 w-3 text-primary-foreground" />
                      </div>
                    )}
                  </div>
                );
              })}
            </RadioGroup>
          </div>

          {/* Data View */}
          <div className="space-y-3">
            <Label className="text-sm font-medium">{t('preferences.dataViewLabel')}</Label>
            <RadioGroup 
              value={formData.dataView} 
              onValueChange={(value: 'table' | 'list') => handleChange('dataView', value)} 
              className="grid grid-cols-2 gap-3"
              disabled={!canEditSettings}
            >
              {dataViews.map(view => {
                const IconComponent = view.icon;
                const isSelected = formData.dataView === view.value;
                return (
                  <div key={view.value} className="relative">
                    <RadioGroupItem value={view.value} id={`view-${view.value}`} className="peer sr-only" disabled={!canEditSettings} />
                    <Label 
                      htmlFor={`view-${view.value}`} 
                      className={`flex items-center gap-3 p-4 border-2 rounded-xl transition-all ${isSelected ? 'border-primary bg-primary/10' : ''} ${!canEditSettings ? 'cursor-not-allowed' : 'cursor-pointer hover:border-primary/50 hover:bg-primary/5'}`}
                    >
                      <IconComponent className={`h-5 w-5 ${isSelected ? 'text-primary' : ''}`} />
                      <div>
                        <span className={`font-medium block ${isSelected ? 'text-primary' : ''}`}>{t(view.labelKey)}</span>
                        <span className="text-xs text-muted-foreground">{t(view.descKey)}</span>
                      </div>
                    </Label>
                    {isSelected && (
                      <div className="absolute -top-1 -right-1 w-5 h-5 bg-primary rounded-full flex items-center justify-center">
                        <Check className="h-3 w-3 text-primary-foreground" />
                      </div>
                    )}
                  </div>
                );
              })}
            </RadioGroup>
          </div>
        </CardContent>
      </Card>

      {/* Save Button - only for users with settings:update permission */}
      {hasChanges && canEditSettings && (
        <div className="flex justify-end">
          <Button onClick={handleSave} disabled={isSaving} className="gradient-primary">
            {isSaving ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Save className="h-4 w-4 mr-2" />}
            Save Changes
          </Button>
        </div>
      )}

      {/* Unsaved Changes Dialog */}
      <UnsavedChangesDialog
        open={showUnsavedDialog}
        onOpenChange={setShowUnsavedDialog}
        onSave={handleDialogSave}
        onDiscard={handleDialogDiscard}
        onCancel={handleDialogCancel}
        isSaving={isSaving}
      />
    </div>
  );
}