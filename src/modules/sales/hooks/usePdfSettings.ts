import { useState, useCallback, useEffect } from 'react';
import { useToast } from '@/hooks/use-toast';
import { PdfSettings, updateNestedObject, colorThemes } from '../utils/pdfSettings.utils';
import { PdfSettingsService } from '../services/pdfSettings.service';

export const usePdfSettings = (initialSettings: PdfSettings, onSettingsChange: (settings: PdfSettings) => void) => {
  const [localSettings, setLocalSettings] = useState<PdfSettings>(initialSettings);
  const [isLoading, setIsLoading] = useState(false);
  const [hasLoadedFromBackend, setHasLoadedFromBackend] = useState(false);
  const { toast } = useToast();

  // Load settings from backend on mount
  useEffect(() => {
    if (!hasLoadedFromBackend) {
      setHasLoadedFromBackend(true);
      PdfSettingsService.loadSettingsAsync()
        .then((settings) => {
          setLocalSettings(settings);
          onSettingsChange(settings);
        })
        .catch(() => {
          // Keep initial settings on error
        });
    }
  }, [hasLoadedFromBackend, onSettingsChange]);

  const updateSettings = useCallback((path: string, value: any) => {
    setLocalSettings(prev => {
      const updated = updateNestedObject(prev, path, value);
      
      // Immediately update parent component for real-time preview
      onSettingsChange(updated);
      PdfSettingsService.saveSettings(updated);
      
      return updated;
    });
  }, [onSettingsChange]);

  const handleSave = useCallback(() => {
    try {
      PdfSettingsService.saveSettings(localSettings);
      toast({
        title: "Settings Saved",
        description: "Your PDF preferences have been saved and synced.",
      });
      return true;
    } catch (error) {
      toast({
        title: "Save Failed",
        description: "Failed to save PDF settings. Please try again.",
        variant: "destructive",
      });
      return false;
    }
  }, [localSettings, toast]);

  const handleReset = useCallback(async () => {
    try {
      setIsLoading(true);
      const resetSettings = await PdfSettingsService.resetSettingsAsync();
      setLocalSettings(resetSettings);
      onSettingsChange(resetSettings);
      toast({
        title: "Settings Reset",
        description: "All settings have been reset to default values.",
      });
    } catch (error) {
      toast({
        title: "Reset Failed",
        description: "Failed to reset settings. Please try again.",
        variant: "destructive",
      });
    } finally {
      setIsLoading(false);
    }
  }, [onSettingsChange, toast]);

  const handleExportSettings = useCallback(() => {
    try {
      PdfSettingsService.exportSettings(localSettings);
      toast({
        title: "Settings Exported",
        description: "Settings have been exported successfully.",
      });
    } catch (error) {
      toast({
        title: "Export Failed",
        description: "Failed to export settings. Please try again.",
        variant: "destructive",
      });
    }
  }, [localSettings, toast]);

  const handleImportSettings = useCallback(() => {
    const input = PdfSettingsService.createFileInput();
    
    input.onchange = async (event) => {
      const file = (event.target as HTMLInputElement).files?.[0];
      if (file) {
        try {
          setIsLoading(true);
          const imported = await PdfSettingsService.importSettings(file);
          setLocalSettings(imported);
          onSettingsChange(imported);
          toast({
            title: "Settings Imported",
            description: "Settings have been imported and synced successfully.",
          });
        } catch (error) {
          toast({
            title: "Import Failed",
            description: error instanceof Error ? error.message : "Failed to import settings file.",
            variant: "destructive",
          });
        } finally {
          setIsLoading(false);
        }
      }
    };
    
    document.body.appendChild(input);
    input.click();
    document.body.removeChild(input);
  }, [onSettingsChange, toast]);

  const applyColorTheme = useCallback((theme: typeof colorThemes[0]) => {
    const updatedSettings = updateNestedObject(localSettings, 'colors', {
      ...localSettings.colors,
      primary: theme.primary,
      secondary: theme.secondary,
      accent: theme.accent,
    });
    
    setLocalSettings(updatedSettings);
    onSettingsChange(updatedSettings);
    PdfSettingsService.saveSettings(updatedSettings);
    
    toast({
      title: "Theme Applied",
      description: `${theme.name} theme has been applied.`,
    });
  }, [localSettings, onSettingsChange, toast]);

  const refreshFromBackend = useCallback(async () => {
    try {
      setIsLoading(true);
      const settings = await PdfSettingsService.refreshFromBackend();
      setLocalSettings(settings);
      onSettingsChange(settings);
      toast({
        title: "Settings Refreshed",
        description: "Settings refreshed from cloud.",
      });
    } catch (error) {
      toast({
        title: "Refresh Failed",
        description: "Failed to refresh settings from cloud.",
        variant: "destructive",
      });
    } finally {
      setIsLoading(false);
    }
  }, [onSettingsChange, toast]);

  return {
    localSettings,
    setLocalSettings,
    updateSettings,
    handleSave,
    handleReset,
    handleExportSettings,
    handleImportSettings,
    applyColorTheme,
    refreshFromBackend,
    isLoading,
  };
};
