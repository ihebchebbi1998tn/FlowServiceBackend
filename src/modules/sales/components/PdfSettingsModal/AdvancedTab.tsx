import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Slider } from '@/components/ui/slider';
import { Switch } from '@/components/ui/switch';
import { Separator } from '@/components/ui/separator';
import { Settings, Zap, Layers, Eye } from 'lucide-react';
import { PdfSettings } from '../../utils/pdfSettings.utils';
import pdfQualityOptions from '@/data/mock/pdf-quality.json';

interface AdvancedTabProps {
  settings: PdfSettings;
  updateSettings: (path: string, value: any) => void;
}

export function AdvancedTab({ settings, updateSettings }: AdvancedTabProps) {
  return (
    <div className="mt-6 space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Zap className="h-4 w-4" />
            Performance Settings
          </CardTitle>
          <CardDescription>Optimize PDF generation and file size</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <Label>Compression: {settings.advanced.compression}%</Label>
              <Slider
                value={[settings.advanced.compression]}
                onValueChange={(value) => updateSettings('advanced.compression', value[0])}
                min={20}
                max={100}
                step={5}
                className="mt-2"
              />
              <p className="text-xs text-muted-foreground mt-1">
                Higher compression = smaller file size
              </p>
            </div>
            <div>
              <Label>Quality</Label>
              <Select 
                value={settings.advanced.quality} 
                onValueChange={(value) => updateSettings('advanced.quality', value)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                  <SelectContent>
                    {pdfQualityOptions.map((q:any) => (
                      <SelectItem key={q.id} value={q.id}>{q.name}</SelectItem>
                    ))}
                  </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Eye className="h-4 w-4" />
            Watermark Settings
          </CardTitle>
          <CardDescription>Add watermark for document security</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="flex items-center justify-between">
            <Label htmlFor="watermarkEnabled" className="cursor-pointer">
              Enable Watermark
            </Label>
            <Switch
              id="watermarkEnabled"
              checked={settings.showElements.watermark}
              onCheckedChange={(checked) => updateSettings('showElements.watermark', checked)}
            />
          </div>

          {settings.showElements.watermark && (
            <>
              <Separator />
              <div className="space-y-4">
                <div>
                  <Label htmlFor="watermarkText">Watermark Text</Label>
                  <Input
                    id="watermarkText"
                    value={settings.advanced.watermarkText}
                    onChange={(e) => updateSettings('advanced.watermarkText', e.target.value)}
                    placeholder="CONFIDENTIAL"
                    className="mt-1"
                  />
                </div>
                <div>
                  <Label>Opacity: {settings.advanced.watermarkOpacity}%</Label>
                  <Slider
                    value={[settings.advanced.watermarkOpacity]}
                    onValueChange={(value) => updateSettings('advanced.watermarkOpacity', value[0])}
                    min={5}
                    max={50}
                    step={5}
                    className="mt-2"
                  />
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Layers className="h-4 w-4" />
            Layout Dimensions
          </CardTitle>
          <CardDescription>Fine-tune header and footer spacing</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <Label>Header Height: {settings.advanced.headerHeight}px</Label>
              <Slider
                value={[settings.advanced.headerHeight]}
                onValueChange={(value) => updateSettings('advanced.headerHeight', value[0])}
                min={40}
                max={150}
                step={5}
                className="mt-2"
              />
            </div>
            <div>
              <Label>Footer Height: {settings.advanced.footerHeight}px</Label>
              <Slider
                value={[settings.advanced.footerHeight]}
                onValueChange={(value) => updateSettings('advanced.footerHeight', value[0])}
                min={30}
                max={120}
                step={5}
                className="mt-2"
              />
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}