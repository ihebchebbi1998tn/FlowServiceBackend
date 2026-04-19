# Frontend Implementation Guide: Offer Sent Tracking

This guide explains how to implement the `SentCount` tracking feature in your React/Next.js frontend. Space these out into three logical steps:

## 1. Update TypeScript Types / Interfaces

First, update your models/types in the frontend to include the new `sentCount` property that the backend now returns. Because APIs typically use camel case for responses, `SentCount` will likely serialize to `sentCount` depending on your backend configuration. 

```typescript
// types/offer.ts (or wherever your Offer interface lives)
export interface Offer {
  id: number;
  offerNumber: string;
  // ... other existing fields
  
  // Add the new field
  sentCount: number;
}
```

## 2. Display the `SentCount` in the Offer Details View

In the component that renders the details of a single offer (e.g., `OfferDetails.tsx` or similar), display the counter. A good place for this is near the Offer Status or Actions.

```tsx
// components/OfferDetails.tsx
import React from 'react';
import { Offer } from '@/types/offer';
import { Send, CheckCircle } from 'lucide-react'; // Example icons

interface OfferDetailsProps {
  offer: Offer;
}

export const OfferDetails: React.FC<OfferDetailsProps> = ({ offer }) => {
  return (
    <div className="p-4 bg-white rounded shadow-sm">
        {/* Existing header */}
        <div className="flex justify-between items-center mb-6">
            <h1 className="text-2xl font-bold">Offer {offer.offerNumber}</h1>
            
            {/* New Sent Count Display */}
            <div className="flex items-center gap-2 text-sm text-gray-600 bg-gray-50 px-3 py-1 rounded-full border">
                <Send className="w-4 h-4 text-blue-500" />
                <span>Sent {offer.sentCount} time{offer.sentCount !== 1 ? 's' : ''}</span>
            </div>
        </div>
        
        {/* ... Rest of your component */}
    </div>
  );
};
```

## 3. Implement the "Mark as Sent" Action

You need to wire up a button that calls the new `POST /api/offers/{id}/send` endpoint.

### API Service Call

Add the API call to your network/services layer.

```typescript
// services/api/offers.ts

export const sendOffer = async (id: number): Promise<Offer> => {
  const response = await fetch(`/api/offers/${id}/send`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      // Include your auth headers here, e.g.:
      // 'Authorization': `Bearer ${token}` 
    },
  });

  if (!response.ok) {
    throw new Error('Failed to mark offer as sent');
  }

  const result = await response.json();
  
  // Note: Backend returns { success: true, data: { ...offer } } based on your controller
  return result.data; 
};
```

### The "Send Offer" Button Component

Add a button to your UI that triggers this API call. If you are using React Query (recommended), it would look something like this. Alternatively, if you are using standard React state, you can handle it with `useState`.

```tsx
// components/SendOfferButton.tsx
import React, { useState } from 'react';
import { sendOffer } from '@/services/api/offers';
import { Offer } from '@/types/offer';

interface SendOfferButtonProps {
  offerId: number;
  // Callback to update the parent component's state with the updated offer
  onSentSuccess: (updatedOffer: Offer) => void; 
}

export const SendOfferButton: React.FC<SendOfferButtonProps> = ({ offerId, onSentSuccess }) => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSend = async () => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Call the API endpoint we created on the backend
      const updatedOffer = await sendOffer(offerId);
      
      // Pass the updated offer (which now has incremented sentCount and updated status)
      // back up to the parent to re-render the UI
      onSentSuccess(updatedOffer);
      
      // Optionally show a toast notification here
      // toast.success("Offer marked as sent successfully");
      
    } catch (err: any) {
      setError(err.message || "Failed to send offer");
      // toast.error(err.message || "Failed to send offer");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex flex-col gap-2">
      <button 
        onClick={handleSend} 
        disabled={isLoading}
        className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
      >
        {isLoading ? 'Sending...' : 'Mark as Sent'}
      </button>
      {error && <span className="text-red-500 text-sm">{error}</span>}
    </div>
  );
};
```

## Summary of How it Works Together

1. The user views the offer details. `offer.sentCount` is currently `0`. The API returned `{ ...offer, sentCount: 0, status: "draft" }`.
2. The user clicks **Mark as Sent**.
3. The frontend makes a `POST` request to `/api/offers/{id}/send`.
4. The backend increments `SentCount` in the database, changes status to `sent`, and returns the updated Offer JSON.
5. In the `onSentSuccess` callback, your React state receives the newly returned offer.
6. React re-renders, and the UI immediately updates to say **"Sent 1 time"** and the status label (if you have one) updates to **"Sent"**.
