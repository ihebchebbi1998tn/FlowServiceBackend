import { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Card, CardContent } from '@/components/ui/card';
import { formatStatValue } from "@/lib/formatters";
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Checkbox } from '@/components/ui/checkbox';
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { 
  Filter,
  Upload,
  FileText,
  Image,
  Download,
  Share2,
  Trash2,
  Eye,
  Calendar,
  User,
  List,
  Table as TableIcon,
  MoreVertical,
  FolderOpen,
  Files,
  Activity
} from 'lucide-react';
import { CollapsibleSearch } from "@/components/ui/collapsible-search";
import { useDocuments } from '../hooks/useDocuments';
import { usePageDropZone } from '../hooks/usePageDropZone';
import { DocumentPreviewModal } from './DocumentPreviewModal';
import { DocumentFilters } from './DocumentFilters';
import { DocumentUploadModal } from './DocumentUploadModal';
import { DocumentThumbnail } from './DocumentThumbnail';
import { Document, DocumentFilters as FilterType } from '../types';
import { DocumentsService } from '../services/documents.service';
import { toast } from 'sonner';

export function DocumentsList() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<'all' | 'offers' | 'sales' | 'services' | 'field'>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [showUpload, setShowUpload] = useState(false);
  const [selectedDocument, setSelectedDocument] = useState<Document | null>(null);
  const [filters, setFilters] = useState<FilterType>({});
  const [viewMode, setViewMode] = useState<'list' | 'table'>('table');
  const [selectedStat, setSelectedStat] = useState<string>('all');
  const [showFilterBar, setShowFilterBar] = useState(false);

  // Selection state for bulk actions
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [showBulkDeleteConfirm, setShowBulkDeleteConfirm] = useState(false);
  const [bulkDeleting, setBulkDeleting] = useState(false);

  // Quick upload state (page-level drag-and-drop)
  const [quickUploading, setQuickUploading] = useState(false);
  const [quickUploadProgress, setQuickUploadProgress] = useState(0);
  const [droppedFiles, setDroppedFiles] = useState<File[]>([]);

  // Only search and other filters trigger API call - NOT the CRM/Field tab
  const apiFilters = useMemo(() => {
    const searchFilter = searchQuery ? { search: searchQuery } : {};
    return { ...searchFilter, ...filters };
  }, [searchQuery, filters]);

  const { documents: allDocuments, stats, loading, refetch, deleteDocument, bulkDeleteDocuments, downloadDocument } = useDocuments(apiFilters);

  // Client-side filtering by module type tabs
  const documents = useMemo(() => {
    if (activeTab === 'all') return allDocuments;
    return allDocuments.filter(doc => doc.moduleType === activeTab);
  }, [allDocuments, activeTab]);

  // Selection helpers
  const toggleSelect = useCallback((id: string) => {
    setSelectedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }, []);

  const toggleSelectAll = useCallback(() => {
    if (selectedIds.size === documents.length) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(documents.map(d => d.id)));
    }
  }, [documents, selectedIds.size]);

  const clearSelection = useCallback(() => setSelectedIds(new Set()), []);

  const handleBulkDelete = useCallback(async () => {
    try {
      setBulkDeleting(true);
      await bulkDeleteDocuments(Array.from(selectedIds));
      setSelectedIds(new Set());
      setShowBulkDeleteConfirm(false);
      refetch();
    } catch {
      // error toast is handled in hook
    } finally {
      setBulkDeleting(false);
    }
  }, [selectedIds, bulkDeleteDocuments, refetch]);

  // Quick upload handler — uploads directly as "general" documents
  const handleQuickUpload = useCallback(async (files: File[]) => {
    try {
      setQuickUploading(true);
      setQuickUploadProgress(0);

      await DocumentsService.quickUpload(files, (info) => {
        setQuickUploadProgress(info.percent);
      });

      setQuickUploadProgress(100);
      toast.success(t('documents.uploadSuccess'));
      refetch();
    } catch (error) {
      console.error('Quick upload error:', error);
      toast.error(t('documents.uploadError'));
    } finally {
      // Brief delay to show 100% before hiding
      setTimeout(() => {
        setQuickUploading(false);
        setQuickUploadProgress(0);
      }, 600);
    }
  }, [refetch, t]);

  // Or open modal with pre-filled files
  const handleDropToModal = useCallback((files: File[]) => {
    setDroppedFiles(files);
    setShowUpload(true);
  }, []);

  // Page-level drag-and-drop
  const { isDragging, dragProps } = usePageDropZone({
    onFilesDropped: handleQuickUpload,
    disabled: quickUploading,
  });

  const handleDeleteDocument = async (document: Document) => {
    const confirmed = window.confirm(t('documents.confirmDelete'));
    if (confirmed) {
      await deleteDocument(document.id);
      refetch();
    }
  };

  const handlePreviewDocument = (document: Document) => {
    setSelectedDocument(document);
  };

  const getFileIcon = (fileType: string) => {
    switch (fileType) {
      case 'pdf': return FileText;
      case 'jpg': case 'jpeg': case 'png': case 'gif': case 'webp': return Image;
      default: return FileText;
    }
  };

  const getModuleColor = (moduleType: string) => {
    switch (moduleType) {
      case 'contacts': return 'text-primary bg-primary/10';
      case 'sales': return 'text-success bg-success/10';
      case 'offers': return 'text-secondary-foreground bg-secondary';
      case 'services': return 'text-warning bg-warning/10';
      case 'projects': return 'text-destructive bg-destructive/10';
      case 'field': return 'text-warning bg-warning/10';
      case 'general': return 'text-muted-foreground bg-muted';
      default: return 'text-muted-foreground bg-muted';
    }
  };

  const formatDate = (date: string | Date) => {
    return new Date(date).toLocaleDateString();
  };

  const statsData = useMemo(() => [
    {
      label: t('documents.totalFiles'),
      value: stats?.totalFiles || 0,
      icon: Files,
      color: "chart-1",
      filter: 'all'
    },
    {
      label: t('documents.offers'),
      value: stats?.byModule?.offers || 0,
      icon: FileText,
      color: "chart-2",
      filter: 'offers'
    },
    {
      label: t('documents.sales'),
      value: stats?.byModule?.sales || 0,
      icon: FolderOpen,
      color: "chart-3",
      filter: 'sales'
    },
    {
      label: t('documents.services'),
      value: stats?.byModule?.services || 0,
      icon: Activity,
      color: "chart-4",
      filter: 'services'
    },
    {
      label: t('documents.field'),
      value: stats?.byModule?.field || 0,
      icon: Activity,
      color: "chart-5",
      filter: 'field'
    }
  ], [stats, t]);

  const handleStatClick = (stat: any) => {
    setSelectedStat(stat.filter);
    setActiveTab(stat.filter);
  };

  if (loading) {
    return (
      <div className="space-y-4 p-4">
        {/* Skeleton header */}
        <div className="flex items-center justify-between">
          <div className="h-6 w-40 rounded bg-muted/60 animate-shimmer bg-[length:200%_100%] bg-gradient-to-r from-muted/60 via-muted/30 to-muted/60" />
          <div className="flex gap-2">
            <div className="h-9 w-24 rounded bg-muted/60 animate-shimmer bg-[length:200%_100%] bg-gradient-to-r from-muted/60 via-muted/30 to-muted/60" />
            <div className="h-9 w-24 rounded bg-muted/60 animate-shimmer bg-[length:200%_100%] bg-gradient-to-r from-muted/60 via-muted/30 to-muted/60" />
          </div>
        </div>
        {/* Skeleton document rows */}
        {[...Array(6)].map((_, i) => (
          <div key={i} className="flex items-center gap-4 p-3 bg-card rounded-lg border border-border">
            <div className="h-10 w-10 rounded bg-muted/60 animate-shimmer bg-[length:200%_100%] bg-gradient-to-r from-muted/60 via-muted/30 to-muted/60 flex-shrink-0" />
            <div className="flex-1 space-y-2">
              <div className="h-4 w-48 rounded bg-muted/60 animate-shimmer bg-[length:200%_100%] bg-gradient-to-r from-muted/60 via-muted/30 to-muted/60" />
              <div className="h-3 w-32 rounded bg-muted/60 animate-shimmer bg-[length:200%_100%] bg-gradient-to-r from-muted/40 via-muted/20 to-muted/40" />
            </div>
            <div className="h-3 w-16 rounded bg-muted/60 animate-shimmer bg-[length:200%_100%] bg-gradient-to-r from-muted/60 via-muted/30 to-muted/60" />
          </div>
        ))}
      </div>
    );
  }

  const Header = () => (
    <div className="flex items-center justify-between p-4 border-b border-border bg-card/50 backdrop-blur">
      <div className="flex items-center gap-3">
        <div className="p-2 rounded-lg bg-primary/10">
          <Files className="h-6 w-6 text-primary" />
        </div>
        <div>
          <h1 className="text-xl font-semibold text-foreground">{t('documents.title', 'Documents')}</h1>
          <p className="text-[11px] text-muted-foreground">{t('documents.subtitle', 'Manage and share files across modules')}</p>
        </div>
      </div>
      <div className="flex items-center gap-2">
        <Button variant="outline" onClick={() => { setDroppedFiles([]); setShowUpload(true); }}>
          <Upload className="mr-2 h-4 w-4" />
          {t('documents.upload')}
        </Button>
      </div>
    </div>
  );

  return (
    <div className="flex flex-col relative" {...dragProps}>
      {/* Drag-and-drop overlay */}
      {isDragging && (
        <div className="absolute inset-0 z-50 flex items-center justify-center bg-primary/5 backdrop-blur-sm border-2 border-dashed border-primary rounded-lg pointer-events-none">
          <div className="flex flex-col items-center gap-3 p-8 bg-card/90 rounded-xl shadow-xl border border-primary/30">
            <div className="p-4 rounded-full bg-primary/10">
              <Upload className="h-10 w-10 text-primary" />
            </div>
            <p className="text-lg font-semibold text-foreground">{t('documents.dropFilesHere')}</p>
            <p className="text-sm text-muted-foreground">{t('documents.quickUploadHint')}</p>
          </div>
        </div>
      )}

      {/* Quick upload progress bar */}
      {quickUploading && (
        <div className="sticky top-0 z-40 bg-card border-b border-border p-3">
          <div className="flex items-center gap-3">
            <Upload className="h-4 w-4 text-primary animate-pulse" />
            <div className="flex-1">
              <div className="flex items-center justify-between text-sm mb-1">
                <span className="text-muted-foreground">{t('documents.uploading')}</span>
                <span className="font-medium">{quickUploadProgress}%</span>
              </div>
              <Progress value={quickUploadProgress} className="h-1.5" />
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <Header />

      {/* Stats Cards */}
      <div className="p-3 sm:p-4 border-b border-border">
        <div className="grid grid-cols-2 md:grid-cols-5 gap-3 sm:gap-4">
          {statsData.map((stat, index) => {
            const isSelected = selectedStat === stat.filter;
            return (
              <Card 
                key={index} 
                className={`shadow-card hover-lift gradient-card group cursor-pointer transition-all hover:shadow-lg ${
                  isSelected 
                    ? 'border-2 border-primary bg-primary/5' 
                    : 'border-0'
                }`}
                onClick={() => handleStatClick(stat)}
              >
                <CardContent className="p-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3 flex-1 min-w-0">
                      <div className={`p-2 rounded-lg transition-all flex-shrink-0 ${
                        isSelected 
                          ? 'bg-primary/20' 
                          : `bg-${stat.color}/10 group-hover:bg-${stat.color}/20`
                      }`}>
                        <stat.icon className={`h-4 w-4 transition-all ${
                          isSelected 
                            ? 'text-primary' 
                            : `text-${stat.color}`
                        }`} />
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-xs text-muted-foreground font-medium truncate">{stat.label}</p>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-bold text-foreground">{formatStatValue(stat.value)}</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      </div>

      {/* Search and Controls */}
      <div className="p-3 sm:p-4 border-b border-border bg-card">
        <div className="flex flex-col gap-3 sm:flex-row sm:gap-4 sm:items-center sm:justify-between">
          <div className="flex gap-2 sm:gap-3 flex-1 w-full items-center">
            <div className="flex-1">
              <CollapsibleSearch 
                placeholder={t('documents.searchDocuments')} 
                value={searchQuery} 
                onChange={setSearchQuery}
                className="w-full"
              />
            </div>
            <div className="relative">
              <Button variant="outline" size="sm" className="h-9 sm:h-10 px-3 sm:px-4 whitespace-nowrap" onClick={() => setShowFilterBar(s => !s)}>
                <Filter className="h-4 w-4 mr-2" />
                {t('documents.filters')}
              </Button>
            </div>
          </div>
          <div className="flex gap-1 sm:gap-2">
            <Button 
              variant={viewMode === 'list' ? 'default' : 'outline'} 
              size="sm" 
              onClick={() => setViewMode('list')} 
              className={`flex-1 sm:flex-none ${viewMode === 'list' ? 'bg-primary text-white hover:bg-primary/90' : ''}`}
            >
              <List className={`h-4 w-4 ${viewMode === 'list' ? 'text-white' : ''}`} />
            </Button>
            <Button 
              variant={viewMode === 'table' ? 'default' : 'outline'} 
              size="sm" 
              onClick={() => setViewMode('table')} 
              className={`flex-1 sm:flex-none ${viewMode === 'table' ? 'bg-primary text-white hover:bg-primary/90' : ''}`}
            >
              <TableIcon className={`h-4 w-4 ${viewMode === 'table' ? 'text-white' : ''}`} />
            </Button>
          </div>
        </div>
      </div>

      {/* Slide-down Document Filters */}
      {showFilterBar && (
        <div className="p-3 sm:p-4 border-b border-border bg-card">
          <DocumentFilters filters={filters} onFiltersChange={setFilters} />
        </div>
      )}

      {/* Bulk action bar */}
      {selectedIds.size > 0 && (
        <div className="sticky top-0 z-30 bg-destructive/10 border-b border-destructive/20 p-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Checkbox
                checked={selectedIds.size === documents.length}
                onCheckedChange={toggleSelectAll}
              />
              <span className="text-sm font-medium text-foreground">
                {t('documents.selectedCount', { count: selectedIds.size })}
              </span>
              <Button variant="ghost" size="sm" onClick={clearSelection} className="text-muted-foreground">
                {t('documents.deselectAll')}
              </Button>
            </div>
            <Button
              variant="destructive"
              size="sm"
              onClick={() => setShowBulkDeleteConfirm(true)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('documents.bulkDelete')}
            </Button>
          </div>
        </div>
      )}

      {/* List/Table View */}
      {viewMode === 'list' ? (
        <div className="p-3 sm:p-4 lg:p-6">
              {documents.length === 0 ? (
                <Card className="p-8 text-center text-[0.85rem]">
                  <div className="space-y-4">
                    <FileText className="h-12 w-12 text-muted-foreground mx-auto" />
                    <div>
                      <h3 className="text-lg font-medium">{t('documents.noDocuments')}</h3>
                      <p className="text-muted-foreground">{t('documents.noDocumentsDescription')}</p>
                    </div>
                    <Button onClick={() => setShowUpload(true)}>
                      <Upload className="h-4 w-4 mr-2" />
                      {t('documents.uploadFiles')}
                    </Button>
                  </div>
                </Card>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {documents.map((document) => {
                    const FileIcon = getFileIcon(document.fileType);
                    return (
                      <Card key={document.id} className={`hover:shadow-md transition-shadow ${selectedIds.has(document.id) ? 'ring-2 ring-primary' : ''}`}>
                        <CardContent className="p-4">
                          <div className="flex items-start justify-between mb-3">
                            <div className="flex items-center gap-3 flex-1">
                              <Checkbox
                                checked={selectedIds.has(document.id)}
                                onCheckedChange={() => toggleSelect(document.id)}
                              />
                              <DocumentThumbnail
                                docId={document.id}
                                fileType={document.fileType}
                                fileName={document.fileName}
                                size="md"
                              />
                              <div className="flex-1 min-w-0">
                                <h3 className="font-medium truncate">{document.fileName}</h3>
                                <Badge variant="secondary" className={getModuleColor(document.moduleType)}>
                                  {t(`documents.${document.moduleType}`)}
                                </Badge>
                              </div>
                            </div>
                            <DropdownMenu>
                              <DropdownMenuTrigger asChild>
                                <Button variant="ghost" size="sm">
                                  <MoreVertical className="h-4 w-4" />
                                </Button>
                              </DropdownMenuTrigger>
                              <DropdownMenuContent align="end">
                                <DropdownMenuItem onClick={() => handlePreviewDocument(document)}>
                                  <Eye className="h-4 w-4 mr-2" />
                                  {t('documents.preview')}
                                </DropdownMenuItem>
                                <DropdownMenuItem onClick={() => downloadDocument(document)}>
                                  <Download className="h-4 w-4 mr-2" />
                                  {t('documents.download')}
                                </DropdownMenuItem>
                                <DropdownMenuSeparator />
                                <DropdownMenuItem 
                                  onClick={() => handleDeleteDocument(document)}
                                  className="text-destructive"
                                >
                                  <Trash2 className="h-4 w-4 mr-2" />
                                  {t('documents.delete')}
                                </DropdownMenuItem>
                              </DropdownMenuContent>
                            </DropdownMenu>
                          </div>
                          <div className="space-y-2 text-sm text-muted-foreground">
                            <div className="flex items-center gap-2">
                              <Calendar className="h-3 w-3" />
                              <span>{formatDate(document.uploadedAt)}</span>
                            </div>
                            <div className="flex items-center gap-2">
                              <User className="h-3 w-3" />
                              <span>{document.uploadedByName}</span>
                            </div>
                            <div className="flex justify-between">
                              <span>{DocumentsService.formatFileSize(document.fileSize)}</span>
                              <span>{document.fileType.toUpperCase()}</span>
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    );
                  })}
                </div>
              )}
        </div>
      ) : (
        <div className="p-3 sm:p-4 lg:p-6">
              {documents.length === 0 ? (
                <Card className="p-8 text-center">
                  <div className="space-y-4">
                    <FileText className="h-12 w-12 text-muted-foreground mx-auto" />
                    <div>
                      <h3 className="text-lg font-medium">{t('documents.noDocuments')}</h3>
                      <p className="text-muted-foreground">{t('documents.noDocumentsDescription')}</p>
                    </div>
                    <Button onClick={() => setShowUpload(true)}>
                      <Upload className="h-4 w-4 mr-2" />
                      {t('documents.uploadFiles')}
                    </Button>
                  </div>
                </Card>
              ) : (
                <Card>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead className="w-[40px]">
                          <Checkbox
                            checked={documents.length > 0 && selectedIds.size === documents.length}
                            onCheckedChange={toggleSelectAll}
                          />
                        </TableHead>
                        <TableHead>{t('documents.fileName')}</TableHead>
                        <TableHead>{t('documents.module')}</TableHead>
                        <TableHead>{t('documents.fileType')}</TableHead>
                        <TableHead>{t('documents.fileSize')}</TableHead>
                        <TableHead>{t('documents.uploadDate')}</TableHead>
                        <TableHead>{t('documents.uploadedBy')}</TableHead>
                        <TableHead className="w-[50px]"></TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {documents.map((document) => {
                        const FileIcon = getFileIcon(document.fileType);
                        return (
                          <TableRow key={document.id} className={selectedIds.has(document.id) ? 'bg-primary/5' : ''}>
                            <TableCell>
                              <Checkbox
                                checked={selectedIds.has(document.id)}
                                onCheckedChange={() => toggleSelect(document.id)}
                              />
                            </TableCell>
                            <TableCell>
                              <div className="flex items-center gap-3">
                                <DocumentThumbnail
                                  docId={document.id}
                                  fileType={document.fileType}
                                  fileName={document.fileName}
                                  size="sm"
                                />
                                <span className="font-medium">{document.fileName}</span>
                              </div>
                            </TableCell>
                            <TableCell>
                              <Badge variant="secondary" className={getModuleColor(document.moduleType)}>
                                {t(`documents.${document.moduleType}`)}
                              </Badge>
                            </TableCell>
                            <TableCell>{document.fileType.toUpperCase()}</TableCell>
                            <TableCell>{DocumentsService.formatFileSize(document.fileSize)}</TableCell>
                            <TableCell>{formatDate(document.uploadedAt)}</TableCell>
                            <TableCell>{document.uploadedByName}</TableCell>
                            <TableCell>
                              <DropdownMenu>
                                <DropdownMenuTrigger asChild>
                                  <Button variant="ghost" size="sm">
                                    <MoreVertical className="h-4 w-4" />
                                  </Button>
                                </DropdownMenuTrigger>
                                <DropdownMenuContent align="end">
                                  <DropdownMenuItem onClick={() => handlePreviewDocument(document)}>
                                    <Eye className="h-4 w-4 mr-2" />
                                    {t('documents.preview')}
                                  </DropdownMenuItem>
                                  <DropdownMenuItem onClick={() => downloadDocument(document)}>
                                    <Download className="h-4 w-4 mr-2" />
                                    {t('documents.download')}
                                  </DropdownMenuItem>
                                  <DropdownMenuSeparator />
                                  <DropdownMenuItem 
                                    onClick={() => handleDeleteDocument(document)}
                                    className="text-destructive"
                                  >
                                    <Trash2 className="h-4 w-4 mr-2" />
                                    {t('documents.delete')}
                                  </DropdownMenuItem>
                                </DropdownMenuContent>
                              </DropdownMenu>
                            </TableCell>
                          </TableRow>
                        );
                      })}
                    </TableBody>
                  </Table>
                </Card>
              )}
        </div>
      )}

      {/* Modals */}
      {selectedDocument && (
        <DocumentPreviewModal
          document={selectedDocument}
          isOpen={!!selectedDocument}
          onClose={() => setSelectedDocument(null)}
        />
      )}

      {showUpload && (
        <DocumentUploadModal
          isOpen={showUpload}
          onClose={() => { setShowUpload(false); setDroppedFiles([]); }}
          onUploadComplete={() => {
            setShowUpload(false);
            setDroppedFiles([]);
            refetch();
          }}
          initialFiles={droppedFiles.length > 0 ? droppedFiles : undefined}
        />
      )}

      {/* Bulk Delete Confirmation Dialog */}
      <AlertDialog open={showBulkDeleteConfirm} onOpenChange={setShowBulkDeleteConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {t('documents.bulkDeleteConfirm', { count: selectedIds.size })}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {t('documents.bulkDeleteDescription', { count: selectedIds.size })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={bulkDeleting}>
              {t('documents.cancel')}
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={handleBulkDelete}
              disabled={bulkDeleting}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {bulkDeleting ? t('documents.uploading') : t('documents.confirmAction')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
