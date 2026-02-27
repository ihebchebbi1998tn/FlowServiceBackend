import React, { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { supportTicketsApi, SupportTicketResponse } from '@/services/api/supportTicketsApi';
import { API_CONFIG } from '@/config/api.config';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { toast } from 'sonner';
import {
  Search,
  RefreshCw,
  AlertTriangle,
  Clock,
  CheckCircle2,
  XCircle,
  Loader2,
  Paperclip,
  ExternalLink,
  Mail,
  Globe,
  FolderOpen,
  Eye,
  FileText,
  Image as ImageIcon,
  Download,
  ArrowUpDown,
  TicketCheck,
  CalendarDays,
  User,
  Tag,
  ChevronRight,
  Inbox,
  ArrowDown,
  ArrowUp,
} from 'lucide-react';

const STATUS_CONFIG: Record<string, { label: string; color: string; icon: React.ElementType; dotColor: string }> = {
  open: { label: 'Open', color: 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20', icon: AlertTriangle, dotColor: 'bg-blue-500' },
  in_progress: { label: 'In Progress', color: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20', icon: Clock, dotColor: 'bg-amber-500' },
  resolved: { label: 'Resolved', color: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20', icon: CheckCircle2, dotColor: 'bg-emerald-500' },
  closed: { label: 'Closed', color: 'bg-muted text-muted-foreground border-border/50', icon: XCircle, dotColor: 'bg-muted-foreground/50' },
};

const URGENCY_CONFIG: Record<string, { label: string; color: string }> = {
  low: { label: 'Low', color: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20' },
  medium: { label: 'Medium', color: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20' },
  high: { label: 'High', color: 'bg-orange-500/10 text-orange-600 dark:text-orange-400 border-orange-500/20' },
  critical: { label: 'Critical', color: 'bg-red-500/10 text-red-600 dark:text-red-400 border-red-500/20' },
};

export default function TicketsAdminPage() {
  const { t } = useTranslation('support');
  const [tickets, setTickets] = useState<SupportTicketResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [urgencyFilter, setUrgencyFilter] = useState('all');
  const [selectedTicket, setSelectedTicket] = useState<SupportTicketResponse | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const [updatingStatus, setUpdatingStatus] = useState<number | null>(null);
  const [sortField, setSortField] = useState<'createdAt' | 'urgency'>('createdAt');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');

  const fetchTickets = useCallback(async () => {
    setLoading(true);
    try {
      const data = await supportTicketsApi.getAll();
      setTickets(data);
    } catch (err) {
      console.error('Failed to fetch tickets:', err);
      toast.error(t('admin.fetchError', 'Failed to load tickets'));
    } finally {
      setLoading(false);
    }
  }, [t]);

  useEffect(() => { fetchTickets(); }, [fetchTickets]);

  const handleStatusChange = async (ticketId: number, newStatus: string) => {
    setUpdatingStatus(ticketId);
    try {
      const updated = await supportTicketsApi.updateStatus(ticketId, newStatus);
      setTickets((prev) => prev.map((tk) => (tk.id === ticketId ? updated : tk)));
      if (selectedTicket?.id === ticketId) setSelectedTicket(updated);
      toast.success(t('admin.statusUpdated', 'Status updated'));
    } catch (err) {
      console.error('Failed to update status:', err);
      toast.error(t('admin.statusError', 'Failed to update status'));
    } finally {
      setUpdatingStatus(null);
    }
  };

  const urgencyOrder: Record<string, number> = { critical: 0, high: 1, medium: 2, low: 3 };

  const filtered = tickets
    .filter((tk) => {
      if (statusFilter !== 'all' && tk.status !== statusFilter) return false;
      if (urgencyFilter !== 'all' && tk.urgency !== urgencyFilter) return false;
      if (search) {
        const q = search.toLowerCase();
        return (
          tk.title.toLowerCase().includes(q) ||
          tk.description.toLowerCase().includes(q) ||
          (tk.userEmail || '').toLowerCase().includes(q) ||
          (tk.tenant || '').toLowerCase().includes(q) ||
          `#${tk.id}`.includes(q)
        );
      }
      return true;
    })
    .sort((a, b) => {
      if (sortField === 'urgency') {
        const diff = (urgencyOrder[a.urgency || 'medium'] ?? 2) - (urgencyOrder[b.urgency || 'medium'] ?? 2);
        return sortDir === 'asc' ? diff : -diff;
      }
      const diff = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
      return sortDir === 'asc' ? diff : -diff;
    });

  const openDetail = (tk: SupportTicketResponse) => {
    setSelectedTicket(tk);
    setDetailOpen(true);
  };

  const getAttachmentUrl = (filePath?: string) => {
    if (!filePath) return '';
    if (filePath.startsWith('http')) return filePath;
    return `${API_CONFIG.baseURL}${filePath}`;
  };

  const isImage = (contentType?: string) => contentType?.startsWith('image/');

  const stats = {
    total: tickets.length,
    open: tickets.filter((t) => t.status === 'open').length,
    inProgress: tickets.filter((t) => t.status === 'in_progress').length,
    resolved: tickets.filter((t) => t.status === 'resolved').length,
    closed: tickets.filter((t) => t.status === 'closed').length,
  };

  const toggleSort = (field: 'createdAt' | 'urgency') => {
    if (sortField === field) {
      setSortDir((d) => (d === 'desc' ? 'asc' : 'desc'));
    } else {
      setSortField(field);
      setSortDir('desc');
    }
  };

  const SortIcon = ({ field }: { field: 'createdAt' | 'urgency' }) => {
    if (sortField !== field) return <ArrowUpDown className="h-3 w-3 opacity-40" />;
    return sortDir === 'desc' ? <ArrowDown className="h-3 w-3" /> : <ArrowUp className="h-3 w-3" />;
  };

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
  };

  const formatDateTime = (dateStr: string) => {
    const d = new Date(dateStr);
    return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  };

  return (
    <TooltipProvider>
      <div className="min-h-screen p-4 sm:p-6 lg:p-8 max-w-[1400px] mx-auto space-y-5">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="h-9 w-9 rounded-lg bg-primary/10 flex items-center justify-center">
              <TicketCheck className="h-4.5 w-4.5 text-primary" />
            </div>
            <div>
              <h1 className="text-lg font-semibold text-foreground">
                {t('admin.title', 'Support Tickets')}
              </h1>
              <p className="text-[12px] text-muted-foreground">
                {t('admin.subtitle', 'Manage and respond to reported issues')}
              </p>
            </div>
          </div>
          <Button variant="outline" size="sm" onClick={fetchTickets} disabled={loading} className="gap-1.5 self-start">
            <RefreshCw className={`h-3.5 w-3.5 ${loading ? 'animate-spin' : ''}`} />
            {t('admin.refresh', 'Refresh')}
          </Button>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 sm:grid-cols-5 gap-2.5">
          {[
            { label: t('admin.total', 'Total'), value: stats.total, dotColor: 'bg-foreground/50', active: statusFilter === 'all', filter: 'all' },
            { label: t('admin.statusOpen', 'Open'), value: stats.open, dotColor: STATUS_CONFIG.open.dotColor, active: statusFilter === 'open', filter: 'open' },
            { label: t('admin.statusInProgress', 'In Progress'), value: stats.inProgress, dotColor: STATUS_CONFIG.in_progress.dotColor, active: statusFilter === 'in_progress', filter: 'in_progress' },
            { label: t('admin.statusResolved', 'Resolved'), value: stats.resolved, dotColor: STATUS_CONFIG.resolved.dotColor, active: statusFilter === 'resolved', filter: 'resolved' },
            { label: t('admin.statusClosed', 'Closed'), value: stats.closed, dotColor: STATUS_CONFIG.closed.dotColor, active: statusFilter === 'closed', filter: 'closed' },
          ].map((s) => (
            <Card
              key={s.filter}
              className={`p-3 flex items-center gap-2.5 shadow-none cursor-pointer transition-all duration-150 hover:border-primary/30 ${
                s.active ? 'border-primary/40 bg-primary/5 ring-1 ring-primary/10' : 'border-border/50'
              }`}
              onClick={() => setStatusFilter(s.filter)}
            >
              <div className={`h-2 w-2 rounded-full ${s.dotColor} shrink-0`} />
              <div className="min-w-0">
                <span className="text-lg font-bold text-foreground leading-none">{s.value}</span>
                <p className="text-[11px] text-muted-foreground mt-0.5 truncate">{s.label}</p>
              </div>
            </Card>
          ))}
        </div>

        {/* Filters Row */}
        <Card className="p-3 shadow-none border-border/50 flex flex-col sm:flex-row gap-2.5 items-stretch sm:items-center">
          <div className="relative flex-1">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
            <Input
              placeholder={t('admin.searchPlaceholder', 'Search by title, email, tenant...')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-8 h-8"
            />
          </div>
          <div className="flex gap-2 items-center">
            <Select value={urgencyFilter} onValueChange={setUrgencyFilter}>
              <SelectTrigger className="w-[130px] h-8 text-[12px]">
                <SelectValue placeholder={t('admin.allUrgencies', 'All Urgencies')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">{t('admin.allUrgencies', 'All Urgencies')}</SelectItem>
                <SelectItem value="low">{t('priorities.low', 'Low')}</SelectItem>
                <SelectItem value="medium">{t('priorities.medium', 'Medium')}</SelectItem>
                <SelectItem value="high">{t('priorities.high', 'High')}</SelectItem>
                <SelectItem value="critical">{t('priorities.critical', 'Critical')}</SelectItem>
              </SelectContent>
            </Select>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button variant="ghost" size="icon-sm" onClick={() => toggleSort('urgency')}>
                  <SortIcon field="urgency" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>Sort by urgency</TooltipContent>
            </Tooltip>
          </div>
        </Card>

        {/* Tickets Table */}
        {loading ? (
          <div className="flex items-center justify-center py-20">
            <Loader2 className="h-6 w-6 animate-spin text-primary" />
          </div>
        ) : filtered.length === 0 ? (
          <Card className="p-16 text-center shadow-none border-dashed flex flex-col items-center gap-3">
            <div className="h-12 w-12 rounded-full bg-muted flex items-center justify-center">
              <Inbox className="h-6 w-6 text-muted-foreground/50" />
            </div>
            <div>
              <p className="text-[14px] font-medium text-foreground">
                {t('admin.noTickets', 'No tickets found')}
              </p>
              <p className="text-[12px] text-muted-foreground mt-0.5">
                {t('admin.noTicketsHint', 'Try adjusting your filters or search query')}
              </p>
            </div>
          </Card>
        ) : (
          <Card className="shadow-none border-border/50 overflow-hidden">
            {/* Table Header */}
            <div className="hidden sm:grid sm:grid-cols-[1fr_140px_100px_100px_120px_40px] gap-3 px-4 py-2.5 bg-muted/40 border-b border-border/50 text-[11px] font-medium text-muted-foreground uppercase tracking-wider">
              <button className="flex items-center gap-1 hover:text-foreground transition-colors text-left" onClick={() => toggleSort('createdAt')}>
                Ticket <SortIcon field="createdAt" />
              </button>
              <span>Email</span>
              <span>Category</span>
              <button className="flex items-center gap-1 hover:text-foreground transition-colors" onClick={() => toggleSort('urgency')}>
                Urgency <SortIcon field="urgency" />
              </button>
              <span>Status</span>
              <span />
            </div>

            {/* Rows */}
            <div className="divide-y divide-border/40">
              {filtered.map((tk) => {
                const statusCfg = STATUS_CONFIG[tk.status] || STATUS_CONFIG.open;
                const urgencyCfg = URGENCY_CONFIG[tk.urgency || 'medium'] || URGENCY_CONFIG.medium;

                return (
                  <div
                    key={tk.id}
                    className="group px-4 py-3 hover:bg-muted/30 transition-colors cursor-pointer sm:grid sm:grid-cols-[1fr_140px_100px_100px_120px_40px] sm:gap-3 sm:items-center"
                    onClick={() => openDetail(tk)}
                  >
                    {/* Ticket info */}
                    <div className="min-w-0 space-y-0.5">
                      <div className="flex items-center gap-1.5">
                        <span className="text-[11px] font-mono text-muted-foreground/50 shrink-0">#{tk.id}</span>
                        <h3 className="text-[13px] font-medium text-foreground truncate group-hover:text-primary transition-colors">
                          {tk.title}
                        </h3>
                      </div>
                      <div className="flex items-center gap-2 text-[11px] text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <CalendarDays className="h-3 w-3" />
                          {formatDate(tk.createdAt)}
                        </span>
                        {tk.tenant && tk.tenant !== 'unknown' && (
                          <span className="flex items-center gap-1">
                            <Globe className="h-3 w-3" />
                            {tk.tenant}
                          </span>
                        )}
                        {tk.attachments?.length > 0 && (
                          <span className="flex items-center gap-1">
                            <Paperclip className="h-3 w-3" />
                            {tk.attachments.length}
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Email */}
                    <div className="hidden sm:block min-w-0">
                      {tk.userEmail ? (
                        <span className="text-[12px] text-muted-foreground truncate block">{tk.userEmail}</span>
                      ) : (
                        <span className="text-[11px] text-muted-foreground/40">—</span>
                      )}
                    </div>

                    {/* Category */}
                    <div className="hidden sm:block">
                      {tk.category ? (
                        <span className="text-[12px] text-foreground capitalize">{tk.category}</span>
                      ) : (
                        <span className="text-[11px] text-muted-foreground/40">—</span>
                      )}
                    </div>

                    {/* Urgency */}
                    <div className="hidden sm:block" onClick={(e) => e.stopPropagation()}>
                      {tk.urgency && (
                        <Badge variant="outline" className={`text-[10px] px-1.5 py-0 h-5 capitalize font-medium ${urgencyCfg.color}`}>
                          {urgencyCfg.label}
                        </Badge>
                      )}
                    </div>

                    {/* Status */}
                    <div className="hidden sm:block" onClick={(e) => e.stopPropagation()}>
                      <Select
                        value={tk.status}
                        onValueChange={(val) => handleStatusChange(tk.id, val)}
                        disabled={updatingStatus === tk.id}
                      >
                        <SelectTrigger className={`h-6 text-[10px] font-medium border-0 px-2 gap-1 ${statusCfg.color} hover:opacity-80`}>
                          {updatingStatus === tk.id ? (
                            <Loader2 className="h-3 w-3 animate-spin" />
                          ) : (
                            <>
                              <div className={`h-1.5 w-1.5 rounded-full ${statusCfg.dotColor}`} />
                              <SelectValue />
                            </>
                          )}
                        </SelectTrigger>
                        <SelectContent>
                          {Object.entries(STATUS_CONFIG).map(([key, cfg]) => (
                            <SelectItem key={key} value={key} className="text-[12px]">
                              <span className="flex items-center gap-1.5">
                                <div className={`h-1.5 w-1.5 rounded-full ${cfg.dotColor}`} />
                                {cfg.label}
                              </span>
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>

                    {/* Arrow */}
                    <div className="hidden sm:flex justify-end">
                      <ChevronRight className="h-4 w-4 text-muted-foreground/30 group-hover:text-primary transition-colors" />
                    </div>

                    {/* Mobile badges */}
                    <div className="flex sm:hidden items-center gap-2 mt-2">
                      {tk.urgency && (
                        <Badge variant="outline" className={`text-[10px] px-1.5 py-0 h-5 capitalize ${urgencyCfg.color}`}>
                          {urgencyCfg.label}
                        </Badge>
                      )}
                      <Badge variant="outline" className={`text-[10px] px-1.5 py-0 h-5 gap-1 ${statusCfg.color}`}>
                        <div className={`h-1.5 w-1.5 rounded-full ${statusCfg.dotColor}`} />
                        {statusCfg.label}
                      </Badge>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Footer */}
            <div className="px-4 py-2.5 bg-muted/20 border-t border-border/40 text-[11px] text-muted-foreground">
              {filtered.length} {filtered.length === 1 ? 'ticket' : 'tickets'} 
              {filtered.length !== tickets.length && ` (filtered from ${tickets.length})`}
            </div>
          </Card>
        )}

        {/* Detail Modal */}
        <Dialog open={detailOpen} onOpenChange={setDetailOpen}>
          <DialogContent className="sm:max-w-[700px] max-h-[90vh] p-0 gap-0 overflow-hidden">
            {selectedTicket && (() => {
              const statusCfg = STATUS_CONFIG[selectedTicket.status] || STATUS_CONFIG.open;
              const urgencyCfg = URGENCY_CONFIG[selectedTicket.urgency || 'medium'] || URGENCY_CONFIG.medium;
              return (
                <>
                  {/* Modal Header */}
                  <div className="px-6 pt-5 pb-4 border-b border-border/40">
                    <div className="flex items-start justify-between gap-4">
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2 mb-1.5">
                          <span className="text-[11px] font-mono text-muted-foreground bg-muted px-1.5 py-0.5 rounded">#{selectedTicket.id}</span>
                          {selectedTicket.urgency && (
                            <Badge variant="outline" className={`text-[10px] px-1.5 py-0 h-5 capitalize font-medium ${urgencyCfg.color}`}>
                              {urgencyCfg.label}
                            </Badge>
                          )}
                        </div>
                        <DialogTitle className="text-[16px] font-semibold leading-snug">
                          {selectedTicket.title}
                        </DialogTitle>
                        <DialogDescription className="text-[12px] mt-1.5 flex flex-wrap gap-x-4 gap-y-1">
                          {selectedTicket.userEmail && (
                            <span className="flex items-center gap-1">
                              <Mail className="h-3 w-3" /> {selectedTicket.userEmail}
                            </span>
                          )}
                          {selectedTicket.tenant && selectedTicket.tenant !== 'unknown' && (
                            <span className="flex items-center gap-1">
                              <Globe className="h-3 w-3" /> {selectedTicket.tenant}
                            </span>
                          )}
                          <span className="flex items-center gap-1">
                            <CalendarDays className="h-3 w-3" /> {formatDateTime(selectedTicket.createdAt)}
                          </span>
                        </DialogDescription>
                      </div>
                    </div>
                  </div>

                  <ScrollArea className="max-h-[calc(90vh-220px)]">
                    <div className="px-6 py-5 space-y-5">
                      {/* Description */}
                      <section>
                        <h4 className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider mb-2">
                          {t('reportIssue.description', 'Description')}
                        </h4>
                        <div className="text-[13px] text-foreground whitespace-pre-wrap leading-relaxed bg-muted/30 rounded-lg p-4 border border-border/30">
                          {selectedTicket.description}
                        </div>
                      </section>

                      {/* Meta Grid */}
                      {(selectedTicket.category || selectedTicket.currentPage || selectedTicket.relatedUrl) && (
                        <section className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
                          {selectedTicket.category && (
                            <div className="flex items-center gap-2.5 p-3 rounded-lg bg-muted/20 border border-border/30">
                              <Tag className="h-4 w-4 text-muted-foreground shrink-0" />
                              <div className="min-w-0">
                                <p className="text-[10px] text-muted-foreground uppercase tracking-wider">{t('reportIssue.category', 'Category')}</p>
                                <p className="text-[13px] font-medium capitalize mt-0.5 truncate">{selectedTicket.category}</p>
                              </div>
                            </div>
                          )}
                          {selectedTicket.currentPage && (
                            <div className="flex items-center gap-2.5 p-3 rounded-lg bg-muted/20 border border-border/30">
                              <FolderOpen className="h-4 w-4 text-muted-foreground shrink-0" />
                              <div className="min-w-0">
                                <p className="text-[10px] text-muted-foreground uppercase tracking-wider">{t('reportIssue.currentPage', 'Page')}</p>
                                <p className="text-[13px] font-mono mt-0.5 truncate">{selectedTicket.currentPage}</p>
                              </div>
                            </div>
                          )}
                          {selectedTicket.relatedUrl && (
                            <div className="flex items-center gap-2.5 p-3 rounded-lg bg-muted/20 border border-border/30 sm:col-span-2">
                              <ExternalLink className="h-4 w-4 text-muted-foreground shrink-0" />
                              <div className="min-w-0">
                                <p className="text-[10px] text-muted-foreground uppercase tracking-wider">{t('reportIssue.relatedUrl', 'Related URL')}</p>
                                <a
                                  href={selectedTicket.relatedUrl}
                                  target="_blank"
                                  rel="noopener noreferrer"
                                  className="text-[13px] text-primary hover:underline mt-0.5 truncate block"
                                >
                                  {selectedTicket.relatedUrl}
                                </a>
                              </div>
                            </div>
                          )}
                        </section>
                      )}

                      {/* Attachments */}
                      {selectedTicket.attachments?.length > 0 && (
                        <section>
                          <h4 className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider mb-2 flex items-center gap-1.5">
                            <Paperclip className="h-3 w-3" />
                            {t('reportIssue.attachments', 'Attachments')} ({selectedTicket.attachments.length})
                          </h4>
                          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                            {selectedTicket.attachments.map((att) => {
                              const url = getAttachmentUrl(att.filePath);
                              const img = isImage(att.contentType);
                              return (
                                <div key={att.id} className="rounded-lg border border-border/40 overflow-hidden bg-muted/10">
                                  {img && (
                                    <a href={url} target="_blank" rel="noopener noreferrer" className="block">
                                      <img
                                        src={url}
                                        alt={att.fileName}
                                        className="w-full h-28 object-cover hover:opacity-90 transition-opacity"
                                      />
                                    </a>
                                  )}
                                  <div className="p-2.5 flex items-center gap-2">
                                    {img ? <ImageIcon className="h-3.5 w-3.5 text-muted-foreground shrink-0" /> : <FileText className="h-3.5 w-3.5 text-muted-foreground shrink-0" />}
                                    <div className="min-w-0 flex-1">
                                      <p className="text-[12px] truncate font-medium">{att.fileName}</p>
                                      <p className="text-[10px] text-muted-foreground">
                                        {att.fileSize < 1024 * 1024
                                          ? `${(att.fileSize / 1024).toFixed(0)} KB`
                                          : `${(att.fileSize / (1024 * 1024)).toFixed(1)} MB`
                                        }
                                      </p>
                                    </div>
                                    <div className="flex gap-0.5 shrink-0">
                                      <Button variant="ghost" size="icon-sm" asChild>
                                        <a href={url} target="_blank" rel="noopener noreferrer" title="View"><Eye className="h-3.5 w-3.5" /></a>
                                      </Button>
                                      <Button variant="ghost" size="icon-sm" asChild>
                                        <a href={url} download={att.fileName} title="Download"><Download className="h-3.5 w-3.5" /></a>
                                      </Button>
                                    </div>
                                  </div>
                                </div>
                              );
                            })}
                          </div>
                        </section>
                      )}
                    </div>
                  </ScrollArea>

                  {/* Footer - Status Controls */}
                  <div className="px-6 py-4 border-t border-border/40 bg-muted/20">
                    <h4 className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider mb-2.5">
                      {t('admin.changeStatus', 'Update Status')}
                    </h4>
                    <div className="flex gap-2 flex-wrap">
                      {Object.entries(STATUS_CONFIG).map(([key, cfg]) => {
                        const isActive = selectedTicket.status === key;
                        return (
                          <Button
                            key={key}
                            variant={isActive ? 'default' : 'outline'}
                            size="sm"
                            className={`gap-1.5 text-[11px] h-7 ${isActive ? '' : 'text-muted-foreground'}`}
                            disabled={isActive || updatingStatus === selectedTicket.id}
                            onClick={() => handleStatusChange(selectedTicket.id, key)}
                          >
                            {updatingStatus === selectedTicket.id ? (
                              <Loader2 className="h-3 w-3 animate-spin" />
                            ) : (
                              <div className={`h-1.5 w-1.5 rounded-full ${isActive ? 'bg-primary-foreground' : cfg.dotColor}`} />
                            )}
                            {cfg.label}
                          </Button>
                        );
                      })}
                    </div>
                  </div>
                </>
              );
            })()}
          </DialogContent>
        </Dialog>
      </div>
    </TooltipProvider>
  );
}
