import axiosInstance from '@/services/api/axiosInstance';

export interface CreateSupportTicketPayload {
  title: string;
  description: string;
  urgency?: string;
  category?: string;
  currentPage?: string;
  relatedUrl?: string;
  userEmail?: string;
  attachments?: File[];
}

export interface SupportTicketAttachmentDto {
  id: number;
  fileName: string;
  filePath?: string;
  fileSize: number;
  contentType?: string;
}

export interface SupportTicketResponse {
  id: number;
  title: string;
  description: string;
  urgency?: string;
  category?: string;
  currentPage?: string;
  relatedUrl?: string;
  tenant: string;
  userEmail?: string;
  status: string;
  createdAt: string;
  attachments: SupportTicketAttachmentDto[];
}

export const supportTicketsApi = {
  async create(payload: CreateSupportTicketPayload): Promise<SupportTicketResponse> {
    const formData = new FormData();
    formData.append('Title', payload.title);
    formData.append('Description', payload.description);
    if (payload.urgency) formData.append('Urgency', payload.urgency);
    if (payload.category) formData.append('Category', payload.category);
    if (payload.currentPage) formData.append('CurrentPage', payload.currentPage);
    if (payload.relatedUrl) formData.append('RelatedUrl', payload.relatedUrl);
    if (payload.userEmail) formData.append('UserEmail', payload.userEmail);

    if (payload.attachments?.length) {
      payload.attachments.forEach((file) => {
        formData.append('Attachments', file, file.name);
      });
    }

    const response = await axiosInstance.post<SupportTicketResponse>(
      '/api/SupportTickets',
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    );
    return response.data;
  },

  async getAll(): Promise<SupportTicketResponse[]> {
    const response = await axiosInstance.get<SupportTicketResponse[]>('/api/SupportTickets');
    return response.data;
  },

  async getById(id: number): Promise<SupportTicketResponse> {
    const response = await axiosInstance.get<SupportTicketResponse>(`/api/SupportTickets/${id}`);
    return response.data;
  },

  async updateStatus(id: number, status: string): Promise<SupportTicketResponse> {
    const response = await axiosInstance.patch<SupportTicketResponse>(
      `/api/SupportTickets/${id}/status`,
      { status }
    );
    return response.data;
  },
};
