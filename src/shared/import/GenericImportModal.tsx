 import { useState } from 'react';
 import { useTranslation } from 'react-i18next';
 import {
   Dialog,
   DialogContent,
   DialogDescription,
   DialogHeader,
   DialogTitle,
 } from '@/components/ui/dialog';
 import { Button } from '@/components/ui/button';
 import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
 import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
 import { Progress } from '@/components/ui/progress';
 import { Alert, AlertDescription } from '@/components/ui/alert';
 import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
 import { Label } from '@/components/ui/label';
 import { Checkbox } from '@/components/ui/checkbox';
 import { Badge } from '@/components/ui/badge';
 import { 
   Upload, 
   Download, 
   FileSpreadsheet, 
   CheckCircle, 
   AlertCircle,
   Check,
   X,
   AlertTriangle,
   ChevronRight,
   ChevronDown
 } from 'lucide-react';
 import type { ImportConfig, ImportPreview, ColumnMapping } from './types';
 import type { UseGenericImportOptions } from './useGenericImport';
 import { useGenericImport } from './useGenericImport';
 import { useRef, useCallback, useMemo, useEffect } from 'react';
 
 interface GenericImportModalProps<T> {
   open: boolean;
   onOpenChange: (open: boolean) => void;
   config: ImportConfig<T>;
   onImport: UseGenericImportOptions<T>['onImport'];
   onSuccess?: () => void;
   translationNamespace: string;
 }
 
 export function GenericImportModal<T>({ 
   open, 
   onOpenChange, 
   config, 
   onImport,
   onSuccess,
   translationNamespace 
}: GenericImportModalProps<T>) {
    const { t } = useTranslation(translationNamespace);
    const [activeTab, setActiveTab] = useState('dynamic');
    const contentRef = useRef<HTMLDivElement>(null);
    
    const importHook = useGenericImport<T>({
      config,
      onImport,
      onSuccess: () => {
        handleClose();
        onSuccess?.();
      }
    });

    // Scroll to top when preview is shown or step changes
    useEffect(() => {
      if (contentRef.current) {
        contentRef.current.scrollTop = 0;
      }
    }, [importHook.preview, importHook.currentStep]);

    const handleClose = () => {
      importHook.resetImport();
      onOpenChange(false);
    };
 
    const handleDownloadEmptyTemplate = () => {
      const localizedHeaders = config.fields.map(f => 
        f.required ? `${t(`bulkImport.fields.${f.key}`, f.label)}*` : t(`bulkImport.fields.${f.key}`, f.label)
      );
      importHook.downloadTemplate(localizedHeaders, undefined); // No example data
    };

    const handleDownloadTemplateWithData = () => {
      const localizedHeaders = config.fields.map(f => 
        f.required ? `${t(`bulkImport.fields.${f.key}`, f.label)}*` : t(`bulkImport.fields.${f.key}`, f.label)
      );
      importHook.downloadTemplate(localizedHeaders, config.exampleData);
    };
 
    return (
      <Dialog open={open} onOpenChange={handleClose}>
        <DialogContent className="w-[95vw] max-w-6xl h-[90vh] sm:h-auto sm:max-h-[90vh] overflow-hidden flex flex-col p-4 sm:p-6">
          <DialogHeader className="flex-shrink-0">
            <DialogTitle className="flex items-center gap-2 text-base sm:text-lg">
              <Upload className="h-4 w-4 sm:h-5 sm:w-5" />
              {t('bulkImport.title', `Import ${config.entityName}`)}
            </DialogTitle>
            <DialogDescription className="text-xs sm:text-sm">
              {t('bulkImport.description', 'Choose your import method and upload your data')}
            </DialogDescription>
          </DialogHeader>
 
          <div ref={contentRef} className="flex-1 overflow-y-auto overflow-x-hidden min-h-0">
            {!importHook.preview ? (
              importHook.currentStep === 'mapping' ? (
                <MappingStep 
                  importHook={importHook} 
                  config={config}
                  t={t}
                />
              ) : (
                <Tabs value={activeTab} onValueChange={setActiveTab} className="h-full flex flex-col">
                  <TabsList className="grid w-full grid-cols-2 flex-shrink-0">
                    <TabsTrigger value="dynamic" className="flex items-center gap-1 sm:gap-2 text-xs sm:text-sm px-2 sm:px-4">
                      <FileSpreadsheet className="h-3 w-3 sm:h-4 sm:w-4" />
                      <span className="truncate">{t('bulkImport.dynamicImport', 'Dynamic Import')}</span>
                    </TabsTrigger>
                    <TabsTrigger value="structured" className="flex items-center gap-1 sm:gap-2 text-xs sm:text-sm px-2 sm:px-4">
                      <Download className="h-3 w-3 sm:h-4 sm:w-4" />
                      <span className="truncate">{t('bulkImport.structuredImport.title', 'Structured Import')}</span>
                    </TabsTrigger>
                  </TabsList>

                  <div className="flex-1 overflow-y-auto min-h-0">
                   <TabsContent value="dynamic" className="mt-6">
                     <Card>
                       <CardHeader>
                         <CardTitle>{t('bulkImport.dynamicImport', 'Dynamic Import')}</CardTitle>
                         <CardDescription>
                           {t('bulkImport.dynamicDescription', 'Upload an Excel file with your own column headers. You\'ll be able to map them to required fields.')}
                         </CardDescription>
                       </CardHeader>
                       <CardContent>
                         <FileUploadZone importHook={importHook} t={t} />
                       </CardContent>
                     </Card>
                   </TabsContent>
 
                   <TabsContent value="structured" className="mt-6">
                     <Card>
                       <CardHeader>
                         <CardTitle>{t('bulkImport.structuredImport.title', 'Structured Import')}</CardTitle>
                         <CardDescription>
                           {t('bulkImport.structuredImport.description', 'Use our predefined template for a seamless import experience.')}
                         </CardDescription>
                       </CardHeader>
                        <CardContent className="space-y-6">
                          <div className="space-y-4">
                            <p className="text-sm text-muted-foreground">
                              {t('bulkImport.structuredImport.step1Description', 'Download our template and fill it with your data.')}
                            </p>
                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                              <Button onClick={handleDownloadEmptyTemplate} variant="outline" className="w-full">
                                <Download className="h-4 w-4 mr-2" />
                                {t('bulkImport.downloadEmptyTemplate', 'Empty Template')}
                              </Button>
                              <Button onClick={handleDownloadTemplateWithData} variant="default" className="w-full">
                                <FileSpreadsheet className="h-4 w-4 mr-2" />
                                {t('bulkImport.downloadTemplateWithData', 'Template with Examples')}
                              </Button>
                            </div>
                          </div>
                         <FileUploadZone importHook={importHook} t={t} />
                       </CardContent>
                     </Card>
                   </TabsContent>
                 </div>
               </Tabs>
             )
           ) : (
             <PreviewStep 
               importHook={importHook}
               t={t}
             />
           )}
         </div>
       </DialogContent>
     </Dialog>
   );
 }
 
 // File Upload Zone Component
 function FileUploadZone({ importHook, t }: { importHook: ReturnType<typeof useGenericImport>; t: any }) {
   const [dragActive, setDragActive] = useState(false);
   const [selectedFile, setSelectedFile] = useState<File | null>(null);
   const fileInputRef = useRef<HTMLInputElement>(null);
 
   const handleFileSelect = useCallback((file: File) => {
     if (!file.name.match(/\.(xlsx|xls)$/i)) {
       alert(t('bulkImport.errors.unsupportedFormat', 'Please select an Excel file (.xlsx or .xls)'));
       return;
     }
     
     const maxSizeMB = 50;
     const fileSizeMB = file.size / (1024 * 1024);
     
     if (fileSizeMB > maxSizeMB) {
       alert(t('bulkImport.errors.fileTooLarge', `File size exceeds ${maxSizeMB}MB limit`));
       return;
     }
     
     setSelectedFile(file);
   }, [t]);
 
   const handleDrag = useCallback((e: React.DragEvent) => {
     e.preventDefault();
     e.stopPropagation();
     if (e.type === 'dragenter' || e.type === 'dragover') {
       setDragActive(true);
     } else if (e.type === 'dragleave') {
       setDragActive(false);
     }
   }, []);
 
   const handleDrop = useCallback((e: React.DragEvent) => {
     e.preventDefault();
     e.stopPropagation();
     setDragActive(false);
     
     const files = Array.from(e.dataTransfer.files);
     if (files.length > 0) handleFileSelect(files[0]);
   }, [handleFileSelect]);
 
   const handleFileInput = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
     const files = Array.from(e.target.files || []);
     if (files.length > 0) handleFileSelect(files[0]);
   }, [handleFileSelect]);
 
   const handleUpload = useCallback(() => {
     if (selectedFile) importHook.processFile(selectedFile);
   }, [selectedFile, importHook]);
 
    return (
      <div className="space-y-3 sm:space-y-4">
        <div
          className={`border-2 border-dashed rounded-lg p-4 sm:p-8 text-center transition-colors ${
            dragActive 
              ? 'border-primary bg-primary/10' 
              : 'border-muted-foreground/25 hover:border-muted-foreground/50'
          }`}
          onDragEnter={handleDrag}
          onDragLeave={handleDrag}
          onDragOver={handleDrag}
          onDrop={handleDrop}
        >
          <div className="flex flex-col items-center gap-3 sm:gap-4">
            <div className="p-3 sm:p-4 rounded-full bg-muted">
              {selectedFile ? (
                 <CheckCircle className="h-6 w-6 sm:h-8 sm:w-8 text-primary" />
              ) : (
                <Upload className="h-6 w-6 sm:h-8 sm:w-8 text-muted-foreground" />
              )}
            </div>
           
            {selectedFile ? (
              <div className="space-y-2 text-center">
                <h3 className="font-semibold text-sm sm:text-base break-all px-2">{selectedFile.name}</h3>
                <p className="text-xs sm:text-sm text-muted-foreground">
                  {(selectedFile.size / 1024 / 1024).toFixed(2)} MB
                </p>
                <Button onClick={handleUpload} disabled={importHook.isLoading} size="sm" className="sm:size-default">
                  {importHook.isLoading ? t('bulkImport.processing', 'Processing...') : t('bulkImport.uploadAndProcess', 'Upload & Process')}
                </Button>
              </div>
            ) : (
              <div className="space-y-2 text-center">
                <h3 className="font-semibold text-sm sm:text-base">{t('bulkImport.dropFile', 'Drop your Excel file here')}</h3>
                <p className="text-xs sm:text-sm text-muted-foreground px-2">
                  {t('bulkImport.orClick', 'Or click to browse and select a file')}
                </p>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".xlsx,.xls"
                  onChange={handleFileInput}
                  className="hidden"
                />
                <Button variant="outline" type="button" size="sm" className="sm:size-default" onClick={() => fileInputRef.current?.click()}>
                  {t('bulkImport.chooseFile', 'Choose File')}
                </Button>
              </div>
            )}
          </div>
       </div>
 
       {importHook.isLoading && (
         <div className="space-y-2">
           <div className="flex items-center justify-between text-sm">
             <span>{importHook.analysisMessage || t('bulkImport.processing', 'Processing...')}</span>
             <span>{importHook.uploadProgress}%</span>
           </div>
           <Progress value={importHook.uploadProgress} />
         </div>
       )}
 
       <Alert>
         <AlertCircle className="h-4 w-4" />
         <AlertDescription>
           <strong>{t('bulkImport.supportedFormats', 'Supported formats')}:</strong> Excel (.xlsx, .xls)
         </AlertDescription>
       </Alert>
     </div>
   );
 }
 
// Mapping Step Component
function MappingStep<T>({ 
  importHook, 
  config,
  t 
}: { 
  importHook: ReturnType<typeof useGenericImport<T>>;
  config: ImportConfig<T>;
  t: any;
}) {
  const handleColumnMapping = useCallback((sourceColumn: string, targetField: string | null) => {
    const newMapping: ColumnMapping = {
      ...importHook.columnMapping,
      [sourceColumn]: targetField === 'skip' ? null : targetField
    };
    importHook.updateColumnMapping(newMapping);
  }, [importHook]);

  // Calculate live validation stats based on current mapping
  const validationStats = useMemo(() => {
    if (importHook.rawData.length === 0 || Object.keys(importHook.columnMapping).length === 0) {
      return { valid: 0, invalid: 0, empty: 0, total: 0, errors: [] as string[] };
    }

    let valid = 0;
    let invalid = 0;
    let empty = 0;
    const errorCounts: Record<string, number> = {};

    importHook.rawData.forEach((row) => {
      const mappedData: Record<string, any> = {};
      let hasRequiredField = false;
      let isEmpty = true;

      Object.entries(importHook.columnMapping).forEach(([sourceColumn, targetField]) => {
        if (targetField && row[sourceColumn] !== undefined) {
          const value = String(row[sourceColumn] || '').trim();
          if (value) {
            mappedData[targetField] = value;
            isEmpty = false;
            if (config.requiredFields.includes(targetField)) {
              hasRequiredField = true;
            }
          }
        }
      });

      if (isEmpty) {
        empty++;
        return;
      }

      if (!hasRequiredField) {
        invalid++;
        const requiredLabels = config.fields
          .filter(f => config.requiredFields.includes(f.key))
          .map(f => f.label);
        const errorKey = `Missing: ${requiredLabels.join(' or ')}`;
        errorCounts[errorKey] = (errorCounts[errorKey] || 0) + 1;
        return;
      }

      // Run field validations
      let hasError = false;
      config.fields.forEach(field => {
        const value = mappedData[field.key];
        const valueStr = value ? String(value).trim() : '';
        
        if (field.validate && valueStr) {
          const error = field.validate(valueStr);
          if (error) {
            hasError = true;
            errorCounts[error] = (errorCounts[error] || 0) + 1;
          }
        }
      });

      // Run custom row validation
      if (!hasError && config.validateRow) {
        const transformedData = config.transformRow(mappedData);
        const validation = config.validateRow(transformedData);
        if (validation.errors.length > 0) {
          hasError = true;
          validation.errors.forEach(error => {
            errorCounts[error] = (errorCounts[error] || 0) + 1;
          });
        }
      }

      if (hasError) {
        invalid++;
      } else {
        valid++;
      }
    });

    // Get top 5 most common errors
    const errors = Object.entries(errorCounts)
      .sort((a, b) => b[1] - a[1])
      .slice(0, 5)
      .map(([error, count]) => `${error} (${count})`);

    return { valid, invalid, empty, total: importHook.rawData.length, errors };
  }, [importHook.rawData, importHook.columnMapping, config]);

   return (
     <div className="space-y-4 sm:space-y-6 h-full flex flex-col">

       {/* Live Validation Summary */}
       {importHook.canProceedToPreview() && validationStats.total > 0 && (
         <Card className="flex-shrink-0 border-dashed">
           <CardContent className="p-3 sm:p-4">
             <div className="flex flex-col sm:flex-row sm:items-center gap-3">
               <div className="flex items-center gap-2">
                 <FileSpreadsheet className="h-4 w-4 text-muted-foreground" />
                 <span className="text-sm font-medium">{t('bulkImport.liveValidation', 'Live Validation')}</span>
               </div>
               <div className="flex flex-wrap gap-2">
                 <Badge variant="secondary" className="bg-primary/10 text-primary">
                   <Check className="h-3 w-3 mr-1" />
                   {validationStats.valid} {t('bulkImport.preview.valid', 'Valid')}
                 </Badge>
                 {validationStats.invalid > 0 && (
                   <Badge variant="destructive">
                     <X className="h-3 w-3 mr-1" />
                     {validationStats.invalid} {t('bulkImport.preview.errors', 'Errors')}
                   </Badge>
                 )}
                 {validationStats.empty > 0 && (
                   <Badge variant="secondary" className="bg-muted text-muted-foreground">
                     {validationStats.empty} {t('bulkImport.empty', 'Empty')}
                   </Badge>
                 )}
               </div>
             </div>
             {validationStats.errors.length > 0 && (
               <div className="mt-3 space-y-1">
                 <span className="text-xs font-medium text-muted-foreground">{t('bulkImport.topErrors', 'Top validation errors:')}</span>
                 <div className="flex flex-wrap gap-1">
                   {validationStats.errors.map((error, idx) => (
                     <span key={idx} className="text-xs text-destructive bg-destructive/10 px-2 py-0.5 rounded">
                       {error}
                     </span>
                   ))}
                 </div>
               </div>
             )}
           </CardContent>
         </Card>
       )}

       <Card className="flex-1 flex flex-col min-h-0 overflow-hidden">
         <CardHeader className="flex-shrink-0 p-3 sm:p-6">
           <CardTitle className="text-base sm:text-lg">{t('bulkImport.columnMapping', 'Column Mapping')}</CardTitle>
           <CardDescription className="text-xs sm:text-sm">
             {t('bulkImport.mapColumnsDescription', 'Map your file columns to the required fields. Unmapped columns will be ignored.')}
           </CardDescription>
         </CardHeader>
         <CardContent className="flex-1 overflow-y-auto min-h-0 p-3 sm:p-6 pt-0 sm:pt-0">
           <div className="grid gap-3 sm:gap-4">
             {importHook.headers.map((header) => (
               <div key={header} className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-4">
                 <div className="flex-1 min-w-0">
                   <Label className="text-xs sm:text-sm font-medium truncate block">
                     {header}
                   </Label>
                 </div>
                 <div className="flex-1">
                   <Select
                     value={importHook.columnMapping[header] || 'skip'}
                     onValueChange={(value) => handleColumnMapping(header, value)}
                   >
                     <SelectTrigger className="h-9 sm:h-10 text-xs sm:text-sm">
                       <SelectValue placeholder={t('bulkImport.selectField', 'Select field...')} />
                     </SelectTrigger>
                     <SelectContent>
                       <SelectItem value="skip">{t('bulkImport.skipColumn', '-- Skip this column --')}</SelectItem>
                       {config.fields.map((field) => (
                         <SelectItem key={field.key} value={field.key}>
                           {t(`bulkImport.fields.${field.key}`, field.label)}{field.required ? ' *' : ''}
                         </SelectItem>
                       ))}
                     </SelectContent>
                   </Select>
                 </div>
               </div>
             ))}
           </div>

           <div className="mt-4 sm:mt-6 pt-3 sm:pt-4 border-t flex flex-col sm:flex-row gap-2">
             <Button variant="outline" onClick={importHook.resetImport} size="sm" className="sm:size-default">
               {t('bulkImport.startOver', 'Start Over')}
             </Button>
             <Button
               onClick={() => importHook.generatePreview()}
               disabled={!importHook.canProceedToPreview()}
               className="flex-1 text-xs sm:text-sm"
               size="sm"
             >
               {importHook.canProceedToPreview() 
                 ? t('bulkImport.generatePreview', 'Generate Preview')
                 : t('bulkImport.mapRequiredFields', 'Map required fields to continue')}
             </Button>
           </div>
         </CardContent>
       </Card>
     </div>
   );
 }
 
 // Preview Step Component  
 function PreviewStep<T>({ 
   importHook,
   t
 }: { 
   importHook: ReturnType<typeof useGenericImport<T>>;
   t: any;
 }) {
   const preview = importHook.preview!;
   const [showAllColumns, setShowAllColumns] = useState(false);
   
   const allValidSelected = preview.rows.filter(row => row.status === 'valid').every(row => row.selected);
   
   // Get visible fields from data
   const fieldKeys = new Set<string>();
   preview.rows.forEach(row => {
     Object.keys(row.data as any).forEach(key => fieldKeys.add(key));
   });
   const fieldsArray = Array.from(fieldKeys);
   const displayFields = showAllColumns ? fieldsArray : fieldsArray.slice(0, 5);
 
    return (
      <div className="h-full flex flex-col space-y-3 sm:space-y-4 min-h-0">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 flex-shrink-0">
          <div>
            <h3 className="text-base sm:text-lg font-semibold">{t('bulkImport.preview.title', 'Import Preview')}</h3>
            <p className="text-xs sm:text-sm text-muted-foreground">
              {t('bulkImport.preview.description', 'Review and validate your data before importing')}
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" onClick={importHook.resetImport} size="sm" className="sm:size-default">
              {t('bulkImport.startOver', 'Start Over')}
            </Button>
            <Button onClick={importHook.executeImport} disabled={importHook.isLoading} size="sm" className="sm:size-default">
              {importHook.isLoading ? t('bulkImport.importing', 'Importing...') : t('bulkImport.importSelected', 'Import Selected')}
            </Button>
          </div>
        </div>

        {/* Summary badges */}
        <div className="flex flex-wrap items-center gap-2 sm:gap-3 p-2 sm:p-3 bg-muted/30 rounded-lg border flex-shrink-0">
          <div className="flex items-center gap-2">
            <Checkbox 
              checked={allValidSelected}
              onCheckedChange={(checked) => importHook.toggleAllRowsSelection(checked === true)}
            />
            <span className="text-xs sm:text-sm font-medium">{t('bulkImport.preview.selectAll', 'Select All Valid')}</span>
          </div>
          
          {fieldsArray.length > 5 && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setShowAllColumns(!showAllColumns)}
              className="h-6 sm:h-7 px-2 text-xs"
            >
              {showAllColumns ? (
                <><ChevronDown className="h-3 w-3 mr-1" /><span className="hidden sm:inline">{t('bulkImport.preview.showLess', 'Show Less')}</span><span className="sm:hidden">Less</span></>
              ) : (
                <><ChevronRight className="h-3 w-3 mr-1" /><span className="hidden sm:inline">{t('bulkImport.preview.showAll', { count: fieldsArray.length, defaultValue: `Show All (${fieldsArray.length})` })}</span><span className="sm:hidden">All ({fieldsArray.length})</span></>
              )}
            </Button>
          )}
          
          <div className="flex-1 min-w-0" />
          
          <div className="flex flex-wrap gap-1 sm:gap-2">
            <Badge variant="secondary" className="bg-primary/10 text-primary text-xs">
              <Check className="h-3 w-3 mr-1" />{preview.validRows} <span className="hidden sm:inline">{t('bulkImport.preview.valid', 'Valid')}</span>
            </Badge>
            {preview.duplicateRows > 0 && (
              <Badge variant="secondary" className="bg-warning/20 text-warning-foreground text-xs">
                <AlertTriangle className="h-3 w-3 mr-1" />{preview.duplicateRows} <span className="hidden sm:inline">{t('bulkImport.preview.duplicates', 'Duplicates')}</span>
              </Badge>
            )}
            {preview.invalidRows > 0 && (
              <Badge variant="destructive" className="text-xs">
                <X className="h-3 w-3 mr-1" />{preview.invalidRows} <span className="hidden sm:inline">{t('bulkImport.preview.errors', 'Errors')}</span>
              </Badge>
            )}
          </div>
        </div>
 
        {/* Data table */}
        <div className="flex-1 overflow-auto border rounded-lg min-h-0">
          <div className="min-w-[600px]">
            <table className="w-full text-xs sm:text-sm">
              <thead className="bg-muted/50 sticky top-0 z-10">
                <tr>
                  <th className="p-1.5 sm:p-2 w-8 sm:w-10"></th>
                  {displayFields.map(field => (
                    <th key={field} className="p-1.5 sm:p-2 text-left font-medium whitespace-nowrap">
                      {field.charAt(0).toUpperCase() + field.slice(1).replace(/([A-Z])/g, ' $1')}
                    </th>
                  ))}
                  <th className="p-1.5 sm:p-2 w-16 sm:w-20 text-center">{t('bulkImport.preview.status', 'Status')}</th>
                  <th className="p-1.5 sm:p-2 min-w-[120px] sm:min-w-[200px] text-left">{t('bulkImport.preview.validationErrors', 'Validation Errors')}</th>
                </tr>
              </thead>
              <tbody>
                {preview.rows.map((row) => (
                  <tr 
                    key={row.id} 
                    className={`border-t hover:bg-muted/30 cursor-pointer ${row.status !== 'valid' ? 'opacity-60' : ''}`}
                    onClick={() => row.status === 'valid' && importHook.toggleRowSelection(row.id)}
                  >
                    <td className="p-1.5 sm:p-2 text-center">
                      {row.status === 'valid' && (
                        <Checkbox checked={row.selected} className="h-3.5 w-3.5 sm:h-4 sm:w-4" />
                      )}
                    </td>
                    {displayFields.map(field => {
                      const value = (row.data as any)[field];
                      const contactId = (row.data as any)['contactId'];
                      const isInternal = contactId === 0 || contactId === '0';
                      
                      // Display '-' for contactId = 0 (internal/non-existent contact)
                      let displayValue: React.ReactNode = value || '-';
                      if (field === 'contactId' && isInternal) {
                        displayValue = '-';
                      } else if (field === 'type' && isInternal) {
                        displayValue = t('bulkImport.internal', 'Internal');
                      }
                      
                      return (
                        <td key={field} className="p-1.5 sm:p-2 truncate max-w-[100px] sm:max-w-[200px]" title={String(value ?? '')}>
                          {displayValue}
                        </td>
                      );
                    })}
                    <td className="p-1.5 sm:p-2 text-center">
                      {row.status === 'valid' ? (
                        <Check className="h-3.5 w-3.5 sm:h-4 sm:w-4 text-primary mx-auto" />
                      ) : row.status === 'duplicate' ? (
                        <span title={row.warnings.join(', ')}>
                          <AlertTriangle className="h-3.5 w-3.5 sm:h-4 sm:w-4 text-warning mx-auto" />
                        </span>
                      ) : (
                        <span title={row.errors.join(', ')}>
                          <X className="h-3.5 w-3.5 sm:h-4 sm:w-4 text-destructive mx-auto" />
                        </span>
                      )}
                    </td>
                    <td className="p-1.5 sm:p-2 text-left">
                      {row.errors.length > 0 && (
                        <div className="flex flex-col gap-0.5 sm:gap-1">
                          {row.errors.map((error, idx) => (
                            <span key={idx} className="text-[10px] sm:text-xs text-destructive bg-destructive/10 dark:bg-destructive/20 px-1.5 sm:px-2 py-0.5 rounded truncate max-w-[100px] sm:max-w-none" title={error}>
                              {error}
                            </span>
                          ))}
                        </div>
                      )}
                      {row.warnings.length > 0 && (
                        <div className="flex flex-col gap-0.5 sm:gap-1 mt-0.5 sm:mt-1">
                          {row.warnings.map((warning, idx) => (
                            <span key={idx} className="text-[10px] sm:text-xs text-warning bg-warning/10 px-1.5 sm:px-2 py-0.5 rounded truncate max-w-[100px] sm:max-w-none" title={warning}>
                              {warning}
                            </span>
                          ))}
                        </div>
                      )}
                      {row.errors.length === 0 && row.warnings.length === 0 && (
                        <span className="text-[10px] sm:text-xs text-muted-foreground">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    );
  }