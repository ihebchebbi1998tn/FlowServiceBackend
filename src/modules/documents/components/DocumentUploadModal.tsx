import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { Card, CardContent } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { Upload, FileText, X, CheckCircle } from 'lucide-react';
import { DocumentsService } from '../services/documents.service';
import { toast } from 'sonner';

interface DocumentUploadModalProps {
  isOpen: boolean;
  onClose: () => void;
  onUploadComplete: () => void;
  /** Pre-selected files from page-level drag-and-drop */
  initialFiles?: File[];
  preselectedModule?: {
    type: string;
    id: string;
    name: string;
  };
}

export function DocumentUploadModal({ 
  isOpen, 
  onClose, 
  onUploadComplete, 
  initialFiles,
  preselectedModule 
}: DocumentUploadModalProps) {
  const { t } = useTranslation();
  const [files, setFiles] = useState<File[]>(initialFiles || []);
  const [uploading, setUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [moduleType, setModuleType] = useState(preselectedModule?.type || 'general');
  const [moduleId, setModuleId] = useState(preselectedModule?.id || '');
  const [moduleName, setModuleName] = useState(preselectedModule?.name || '');
  const [description, setDescription] = useState('');
  const [tags, setTags] = useState('');
  const [category, setCategory] = useState<'crm' | 'field'>('crm');
  const [isPublic, setIsPublic] = useState(false);

  // Sync initialFiles when they change (e.g. drag-drop reopens modal)
  const prevInitialRef = useCallback((newFiles: File[] | undefined) => {
    if (newFiles && newFiles.length > 0) {
      setFiles(newFiles);
    }
  }, []);

  // If modal opens with new initial files, set them
  if (initialFiles && initialFiles.length > 0 && files.length === 0) {
    prevInitialRef(initialFiles);
  }

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(event.target.files || []);
    setFiles(prev => [...prev, ...selectedFiles]);
  };

  const handleFileDrop = useCallback((event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    const droppedFiles = Array.from(event.dataTransfer.files);
    setFiles(prev => [...prev, ...droppedFiles]);
  }, []);

  const handleDragOver = useCallback((event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
  }, []);

  const removeFile = (index: number) => {
    setFiles(prev => prev.filter((_, i) => i !== index));
  };

  const handleUpload = async () => {
    if (files.length === 0) {
      toast.error(t('documents.selectFiles'));
      return;
    }

    try {
      setUploading(true);
      setUploadProgress(0);

      await DocumentsService.uploadDocuments(
        {
          files,
          moduleType: moduleType || 'general',
          moduleId: moduleId.trim() || undefined,
          moduleName: moduleName.trim() || undefined,
          description: description.trim() || undefined,
          tags: tags.trim() ? tags.split(',').map(tag => tag.trim()) : [],
          category,
          isPublic,
        },
        (info) => setUploadProgress(info.percent),
      );

      setUploadProgress(100);
      toast.success(t('documents.uploadSuccess'));
      onUploadComplete();
      handleClose();
    } catch (error) {
      console.error('Upload error:', error);
      toast.error(t('documents.uploadError'));
    } finally {
      setUploading(false);
      setUploadProgress(0);
    }
  };

  const handleClose = () => {
    if (!uploading) {
      setFiles([]);
      setModuleType(preselectedModule?.type || 'general');
      setModuleId(preselectedModule?.id || '');
      setModuleName(preselectedModule?.name || '');
      setDescription('');
      setTags('');
      setCategory('crm');
      setIsPublic(false);
      setUploadProgress(0);
      onClose();
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Upload className="h-5 w-5" />
            {t('documents.uploadFiles')}
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          {/* File Upload Area */}
          <div>
            <Label>{t('documents.selectFiles')}</Label>
            <div
              className="mt-2 border-2 border-dashed border-muted-foreground/25 rounded-lg p-6 text-center hover:border-muted-foreground/50 transition-colors cursor-pointer"
              onDrop={handleFileDrop}
              onDragOver={handleDragOver}
              onClick={() => document.getElementById('file-upload-modal')?.click()}
            >
              <Upload className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <p className="text-lg font-medium mb-2">{t('documents.dropFilesHere')}</p>
              <p className="text-sm text-muted-foreground mb-2">{t('documents.supportedFormats')}</p>
              <p className="text-xs text-muted-foreground">{t('documents.maxFileSize')}</p>
            </div>
            <input
              id="file-upload-modal"
              type="file"
              multiple
              className="hidden"
              onChange={handleFileSelect}
            />
          </div>

          {/* Selected Files */}
          {files.length > 0 && (
            <div className="space-y-2">
              <Label>{t('documents.selectedFiles', { count: files.length })}</Label>
              <div className="max-h-40 overflow-y-auto space-y-2">
                {files.map((file, index) => (
                  <Card key={`${file.name}-${index}`}>
                    <CardContent className="p-3">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <FileText className="h-4 w-4 text-muted-foreground" />
                          <div>
                            <p className="text-sm font-medium">{file.name}</p>
                            <p className="text-xs text-muted-foreground">
                              {DocumentsService.formatFileSize(file.size)}
                            </p>
                          </div>
                        </div>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={(e) => { e.stopPropagation(); removeFile(index); }}
                          disabled={uploading}
                        >
                          <X className="h-4 w-4" />
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          )}

          {/* Upload Progress */}
          {uploading && (
            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">{t('documents.uploading')}</span>
                <span className="font-medium">{uploadProgress}%</span>
              </div>
              <Progress value={uploadProgress} className="h-2" />
            </div>
          )}

          {/* Upload Details */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="module-type">{t('documents.module')}</Label>
              <Select 
                value={moduleType} 
                onValueChange={setModuleType}
                disabled={!!preselectedModule}
              >
                <SelectTrigger id="module-type">
                  <SelectValue placeholder={t('documents.select_module')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="general">{t('documents.general')}</SelectItem>
                  <SelectItem value="contacts">{t('documents.contacts')}</SelectItem>
                  <SelectItem value="sales">{t('documents.sales')}</SelectItem>
                  <SelectItem value="offers">{t('documents.offers')}</SelectItem>
                  <SelectItem value="services">{t('documents.services')}</SelectItem>
                  <SelectItem value="projects">{t('documents.projects')}</SelectItem>
                  <SelectItem value="field">{t('documents.field')}</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="category">{t('documents.categories')}</Label>
              <Select value={category} onValueChange={(value: 'crm' | 'field') => setCategory(value)}>
                <SelectTrigger id="category">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="crm">{t('documents.crmFiles')}</SelectItem>
                  <SelectItem value="field">{t('documents.fieldFiles')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Module Record fields - only show when a specific module is selected */}
          {moduleType && moduleType !== 'general' && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="module-id">{t('documents.associatedRecord')} (ID)</Label>
                <Input
                  id="module-id"
                  value={moduleId}
                  onChange={(e) => setModuleId(e.target.value)}
                  placeholder={t('documents.recordIdPlaceholder')}
                  disabled={!!preselectedModule}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="module-name">{t('documents.associatedRecord')}</Label>
                <Input
                  id="module-name"
                  value={moduleName}
                  onChange={(e) => setModuleName(e.target.value)}
                  placeholder={t('documents.recordNamePlaceholder')}
                  disabled={!!preselectedModule}
                />
              </div>
            </div>
          )}

          <div className="space-y-2">
            <Label htmlFor="description">{t('documents.description')}</Label>
            <Textarea
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder={t('documents.description_placeholder')}
              rows={3}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="tags">{t('documents.tags')}</Label>
            <Input
              id="tags"
              value={tags}
              onChange={(e) => setTags(e.target.value)}
              placeholder={t('documents.tags_placeholder')}
            />
          </div>

          <div className="flex items-center space-x-2">
            <Switch
              id="is-public"
              checked={isPublic}
              onCheckedChange={setIsPublic}
            />
            <Label htmlFor="is-public">{t('documents.isPublic')}</Label>
          </div>

          {/* Actions */}
          <div className="flex justify-end space-x-2 pt-4">
            <Button variant="outline" onClick={handleClose} disabled={uploading}>
              {t('documents.cancel')}
            </Button>
            <Button onClick={handleUpload} disabled={uploading || files.length === 0}>
              {uploading ? (
                <>
                  <Upload className="h-4 w-4 mr-2 animate-spin" />
                  {t('documents.uploading')} ({uploadProgress}%)
                </>
              ) : (
                <>
                  <CheckCircle className="h-4 w-4 mr-2" />
                  {t('documents.upload')} ({files.length})
                </>
              )}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
