import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Slider } from '@/components/ui/slider';
import { Switch } from '@/components/ui/switch';
import { Separator } from '@/components/ui/separator';
import { FileText, Layout, Smartphone } from 'lucide-react';
import { PdfSettings, paperSizes, templateStyles } from '../../utils/pdfSettings.utils';

interface LayoutTabProps {
  settings: PdfSettings;
  updateSettings: (path: string, value: any) => void;
}

export function LayoutTab({ settings, updateSettings }: LayoutTabProps) {
  return (
    <div className="mt-6 space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-4 w-4" />
            Document Format
          </CardTitle>
          <CardDescription>Configure page setup and document structure</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <Label>Paper Size</Label>
              <Select 
                value={settings.paperSize} 
                onValueChange={(value) => updateSettings('paperSize', value)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {paperSizes.map((size) => (
                    <SelectItem key={size.value} value={size.value}>
                      {size.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label>Template Style</Label>
              <Select 
                value={settings.templateStyle} 
                onValueChange={(value) => updateSettings('templateStyle', value)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {templateStyles.map((style) => (
                    <SelectItem key={style.value} value={style.value}>
                      {style.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <Separator />

          <div className="space-y-4">
            <h4 className="font-medium flex items-center gap-2">
              <Layout className="h-4 w-4" />
              Margins (mm)
            </h4>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <div>
                <Label>Top: {settings.margins.top}mm</Label>
                <Slider
                  value={[settings.margins.top]}
                  onValueChange={(value) => updateSettings('margins.top', value[0])}
                  min={10}
                  max={50}
                  step={1}
                  className="mt-2"
                />
              </div>
              <div>
                <Label>Bottom: {settings.margins.bottom}mm</Label>
                <Slider
                  value={[settings.margins.bottom]}
                  onValueChange={(value) => updateSettings('margins.bottom', value[0])}
                  min={10}
                  max={50}
                  step={1}
                  className="mt-2"
                />
              </div>
              <div>
                <Label>Left: {settings.margins.left}mm</Label>
                <Slider
                  value={[settings.margins.left]}
                  onValueChange={(value) => updateSettings('margins.left', value[0])}
                  min={10}
                  max={50}
                  step={1}
                  className="mt-2"
                />
              </div>
              <div>
                <Label>Right: {settings.margins.right}mm</Label>
                <Slider
                  value={[settings.margins.right]}
                  onValueChange={(value) => updateSettings('margins.right', value[0])}
                  min={10}
                  max={50}
                  step={1}
                  className="mt-2"
                />
              </div>
            </div>
          </div>

          <Separator />

          <div className="space-y-4">
            <h4 className="font-medium flex items-center gap-2">
              <Smartphone className="h-4 w-4" />
              Mobile Optimization
            </h4>
            <div className="flex items-center justify-between">
              <Label htmlFor="mobileOptimized" className="cursor-pointer">
                Enable mobile-friendly formatting
              </Label>
              <Switch
                id="mobileOptimized"
                checked={settings.mobileOptimized}
                onCheckedChange={(checked) => updateSettings('mobileOptimized', checked)}
              />
            </div>
          </div>

          <Separator />

          <div className="space-y-4">
            <h4 className="font-medium">Currency & Format</h4>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div>
                <Label htmlFor="currencySymbol">Currency Symbol</Label>
                <Input
                  id="currencySymbol"
                  value={settings.currencySymbol}
                  onChange={(e) => updateSettings('currencySymbol', e.target.value)}
                  className="mt-1"
                />
              </div>
              <div>
                <Label>Tax Rate: {settings.taxRate}%</Label>
                <Slider
                  value={[settings.taxRate]}
                  onValueChange={(value) => updateSettings('taxRate', value[0])}
                  min={0}
                  max={30}
                  step={0.5}
                  className="mt-2"
                />
              </div>
              <div>
                <Label htmlFor="dateFormat">Date Format</Label>
                <Select 
                  value={settings.dateFormat} 
                  onValueChange={(value) => updateSettings('dateFormat', value)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="en-US">US (MM/DD/YYYY)</SelectItem>
                    <SelectItem value="de-DE">German (DD.MM.YYYY)</SelectItem>
                    <SelectItem value="en-GB">UK (DD/MM/YYYY)</SelectItem>
                    <SelectItem value="fr-FR">French (DD/MM/YYYY)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}